using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using System;
using System.Linq;
using System.Collections.Generic;
using TransparentApiClient.Google.Core;
using TransparentApiClient.Google.PubSub.V1.Schema;
using TransparentApiClient.Google.PubSub.V1.Resources;

namespace AzureFunctions.Extensions.GooglePubSub {
    internal class Listener : IListener {

        private ITriggeredFunctionExecutor executor;
        private readonly GooglePubSubTriggerAttribute triggerAttribute;

        public Listener(ITriggeredFunctionExecutor executor, GooglePubSubTriggerAttribute triggerAttribute) {
            this.executor = executor;
            this.triggerAttribute = GooglePubSubTriggerAttribute.GetAttributeByConfiguration(triggerAttribute);
        }

        void IListener.Cancel() {
        }

        public void Dispose() {
        }

        async Task IListener.StartAsync(CancellationToken cancellationToken) {

            var credentials = CreatorService.GetCredentials(triggerAttribute);
            var topicsClient = new Topics(credentials);
            var subscriptionsClient = new Subscriptions(credentials);

            if (triggerAttribute.CreateSubscriptionIfDoesntExist) {
                await CreateSubscription(topicsClient, triggerAttribute, cancellationToken);
            }

            var index = 0;
            while (!cancellationToken.IsCancellationRequested) {
                await ListenerPull(topicsClient, subscriptionsClient, index, cancellationToken);
            }

        }

        Task IListener.StopAsync(CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }

        private Task CreateSubscription(Topics topicsClient, GooglePubSubTriggerAttribute triggerAttribute, CancellationToken cancellationToken) {

            var topicName = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}";

            var topic = new TransparentApiClient.Google.PubSub.V1.Schema.Topic() {
                name = topicName
                //ackDeadlineSeconds= triggerAttribute.AcknowledgeDeadline,
                //name = triggerAttribute.SubscriptionId,
                //topic = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}"
            };

            return topicsClient.CreateAsync(topicName, topic, null, cancellationToken);

        }

        private Task ListenerPull(Topics topicsClient, Subscriptions subscriptionsClient, int index, CancellationToken cancellationToken) {

            return GetTypeInput(subscriptionsClient, cancellationToken)
                .ContinueWith((typeInputTask) => {

                    if (typeInputTask.IsFaulted) {
                        throw typeInputTask.Exception;
                    }

                    (TriggeredFunctionData, IEnumerable<string>) buckets = typeInputTask.Result;
                    var (input, ackIds) = buckets;

                    if (input != null && ackIds != null && ackIds.Any()) {

                        var t = executor.TryExecuteAsync(input, cancellationToken)
                                .ContinueWith((functionResultTask) => {

                                    FunctionResult functionResult = functionResultTask.Result;
                                    if (functionResult.Succeeded) {
                                        return AcknowledgeAsync(subscriptionsClient, ackIds, cancellationToken);
                                    }

                                    return Task.CompletedTask;

                                }, cancellationToken).Unwrap();

                        return t;
                    }

                    return Task.CompletedTask;

                }).Unwrap();

        }

        private Task AcknowledgeAsync(Subscriptions subscriptionsClient, IEnumerable<string> ackIds, CancellationToken cancellationToken) {

            return subscriptionsClient.AcknowledgeAsync($"projects/{triggerAttribute.ProjectId}/subscriptions/{triggerAttribute.SubscriptionId}",
                new AcknowledgeRequest() {
                    ackIds = ackIds
                },
                null, cancellationToken);

        }

        private Task<(TriggeredFunctionData messages, IEnumerable<string> ackIds)> GetTypeInput(Subscriptions subscriptionsClient, CancellationToken cancellationToken) {

            return subscriptionsClient.PullAsync($"projects/{triggerAttribute.ProjectId}/subscriptions/{triggerAttribute.SubscriptionId}",
                    new PullRequest() {
                        maxMessages = triggerAttribute.MaxBatchSize,
                        returnImmediately = false
                    },
                    null,
                    cancellationToken)
                .ContinueWith(getMessages);

        }

        private (TriggeredFunctionData messages, IEnumerable<string> ackIds) getMessages(Task<BaseResponse<PullResponse>> pullTask) {

            var pull = pullTask.Result;

            if (pull != null && pull.Success && pull.Response.receivedMessages != null && pull.Response.receivedMessages.Count() > 0) {

                IEnumerable<string> messages = pull.Response.receivedMessages.Select(c => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(c.message.data)));
                IEnumerable<string> ackIds = pull.Response.receivedMessages.Select(c => c.ackId);

                TriggeredFunctionData input = new TriggeredFunctionData {
                    TriggerValue = messages
                };

                return (input, ackIds);
            } else {
                if (pull != null && (!pull.Success)) {
                    throw new Exception(pull.ErrorText);
                }
            }

            return (null, null);
        }

    }
}