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
            //, triggerAttribute.ProjectId, triggerAttribute.SubscriptionId
            var topicsClient = new TransparentApiClient.Google.PubSub.V1.Resources.Topics(credentials);
            var subscriptionsClient = new TransparentApiClient.Google.PubSub.V1.Resources.Subscriptions(credentials);

            await CreateSubscription(topicsClient, triggerAttribute, cancellationToken);

            //var processorCount = Math.kMax(2, Environment.ProcessorCount);

            var index = 0;
            //Parallel.For(1, processorCount, async (t) => {
            while (!cancellationToken.IsCancellationRequested) {
                await ListenerPull(topicsClient, subscriptionsClient, index, cancellationToken);
            }
            //});

        }

        Task IListener.StopAsync(CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }

        private Task CreateSubscription(TransparentApiClient.Google.PubSub.V1.Resources.Topics topicsClient, GooglePubSubTriggerAttribute triggerAttribute, CancellationToken cancellationToken) {

            var topicName = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}";

            var topic = new TransparentApiClient.Google.PubSub.V1.Schema.Topic() {
                name = topicName
                //ackDeadlineSeconds= triggerAttribute.AcknowledgeDeadline,
                //name = triggerAttribute.SubscriptionId,
                //topic = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}"
            };

            return topicsClient.CreateAsync(topicName, topic, null, cancellationToken);

        }

        private Task ListenerPull(TransparentApiClient.Google.PubSub.V1.Resources.Topics topicsClient, TransparentApiClient.Google.PubSub.V1.Resources.Subscriptions subscriptionsClient, int index, CancellationToken cancellationToken) {

            return GetTypeInput(subscriptionsClient, cancellationToken)
                .ContinueWith((typeInputTask) => {
                    (TriggeredFunctionData, IEnumerable<string>) buckets = typeInputTask.Result;

                    //if (buckets != null && buckets.Any()) 
                    {
                        var (input, ackIds) = buckets;
                        //var listExecutions = new List<Task>();
                        //foreach ((TriggeredFunctionData input, IEnumerable<string> ackIds) in buckets) {
                        if (input != null && ackIds != null && ackIds.Any()) {

                            //System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss:fff} - {index} - received {ackIds.Count()} messages");

                            var t = executor.TryExecuteAsync(input, cancellationToken)
                                    .ContinueWith((functionResultTask) => {

                                        FunctionResult functionResult = functionResultTask.Result;
                                        if (functionResult.Succeeded) {
                                            return AcknowledgeAsync(subscriptionsClient, ackIds, cancellationToken);
                                        }

                                        return Task.CompletedTask;

                                    }, cancellationToken).Unwrap();

                            return t;
                            //    listExecutions.Add(t);

                            //}
                        }

                        //return Task.WhenAll(listExecutions)
                        //    .ContinueWith((listExecutionsTask) => {
                        //        if (listExecutionsTask.IsFaulted) {
                        //            throw listExecutionsTask.Exception.InnerException;
                        //        }
                        //    });
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

        private Task<(TriggeredFunctionData messages, IEnumerable<string> ackIds)> GetTypeInput(TransparentApiClient.Google.PubSub.V1.Resources.Subscriptions subscriptionsClient, CancellationToken cancellationToken) {

            return subscriptionsClient.PullAsync($"projects/{triggerAttribute.ProjectId}/subscriptions/{triggerAttribute.SubscriptionId}",
                    new TransparentApiClient.Google.PubSub.V1.Schema.PullRequest() {
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

                //var list = new List<(TriggeredFunctionData messages, IEnumerable<string> ackIds)>();
                IEnumerable<string> messages = pull.Response.receivedMessages.Select(c => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(c.message.data)));
                IEnumerable<string> ackIds = pull.Response.receivedMessages.Select(c => c.ackId);

                TriggeredFunctionData input = new TriggeredFunctionData {
                    TriggerValue = messages
                };

                //list.Add((input, ackIds));

                //return list;
                return (input, ackIds);
            }

            return (null, null);
        }

    }
}