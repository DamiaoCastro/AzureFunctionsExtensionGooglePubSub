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
using Microsoft.Extensions.Logging;
using AzureFunctions.Extensions.GooglePubSub.Services;

namespace AzureFunctions.Extensions.GooglePubSub.Bindings {
    internal class Listener : IListener {

        private readonly ITriggeredFunctionExecutor executor;
        private readonly IServiceFactory serviceFactory;
        private readonly GooglePubSubTriggerAttribute triggerAttribute;

        private const int NumberOfMessageBlocks = 50;

        public Listener(ITriggeredFunctionExecutor executor, GooglePubSubTriggerAttribute triggerAttribute, IServiceFactory serviceFactory) {
            this.executor = executor;
            this.serviceFactory = serviceFactory;
            this.triggerAttribute = triggerAttribute;
        }

        void IListener.Cancel() { }

        public void Dispose() { }

        async Task IListener.StartAsync(CancellationToken cancellationToken) {

            //
            await Task.Yield();

            Parallel.For(0, 1, async (int i) => {
                await CreateListenerAndLoop(cancellationToken).ConfigureAwait(false);
            });

        }

        private async Task CreateListenerAndLoop(CancellationToken cancellationToken) {

        retry:

            try {

                ITopics topicsClient = serviceFactory.GetService<ITopics>(triggerAttribute);

                if (triggerAttribute.CreateSubscriptionIfDoesntExist) {
                    await CreateSubscription(topicsClient, triggerAttribute, cancellationToken).ConfigureAwait(false);
                }

                ISubscriptions subscriptionsClient = serviceFactory.GetService<ISubscriptions>(triggerAttribute);

                while (!cancellationToken.IsCancellationRequested) {
                    var t =
                        ListenerPull(subscriptionsClient, cancellationToken)
                            .ContinueWith((Task<bool> returnTask) => {

                                if (returnTask.Result) {
                                    return Task.CompletedTask;
                                } else {
                                    return Task.Delay(1000);
                                }
                            }, cancellationToken);

                    await t.Unwrap().ConfigureAwait(false);
                }

            } catch (Exception) { }

            if (!cancellationToken.IsCancellationRequested) {
                Thread.Sleep(10 * 1000);
                goto retry;
            }

        }

        Task IListener.StopAsync(CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }

        private Task CreateSubscription(ITopics topicsClient, GooglePubSubTriggerAttribute triggerAttribute, CancellationToken cancellationToken) {

            var topicName = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}";

            var topic = new TransparentApiClient.Google.PubSub.V1.Schema.Topic() {
                name = topicName
                //ackDeadlineSeconds= triggerAttribute.AcknowledgeDeadline,
                //name = triggerAttribute.SubscriptionId,
                //topic = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}"
            };

            return topicsClient.CreateAsync(topicName, topic, null, cancellationToken);

        }

        private async Task<bool> ListenerPull(ISubscriptions subscriptionsClient, CancellationToken cancellationToken) {

            var typeInput = await GetTypeInput(subscriptionsClient, cancellationToken).ConfigureAwait(false);

            if (typeInput.Any()) {

                var tasks = from c in typeInput
                            let messages = c.messages
                            let ackIds = c.ackIds
                            select ExecuteMessagesAsyn(subscriptionsClient, messages, ackIds, cancellationToken);

                await Task.WhenAll(tasks).ConfigureAwait(false);

                return true;
            }

            return false;
        }

        private async Task ExecuteMessagesAsyn(ISubscriptions subscriptionsClient, TriggeredFunctionData messages, IEnumerable<string> ackIds, CancellationToken cancellationToken) {

            if (messages != null && ackIds != null && ackIds.Any()) {
                var functionResult = await executor.TryExecuteAsync(messages, cancellationToken).ConfigureAwait(false);
                if (functionResult.Succeeded) {
                    await AcknowledgeAsync(subscriptionsClient, ackIds, cancellationToken).ConfigureAwait(false);
                }
            }

        }

        private Task AcknowledgeAsync(ISubscriptions subscriptionsClient, IEnumerable<string> ackIds, CancellationToken cancellationToken) {

            return subscriptionsClient.AcknowledgeAsync($"projects/{triggerAttribute.ProjectId}/subscriptions/{triggerAttribute.SubscriptionId}",
                new AcknowledgeRequest() {
                    ackIds = ackIds
                },
                null, cancellationToken);

        }

        private Task<IEnumerable<(TriggeredFunctionData messages, IEnumerable<string> ackIds)>> GetTypeInput(ISubscriptions subscriptionsClient, CancellationToken cancellationToken) {

            var maxMessages = triggerAttribute.MaxBatchSize * NumberOfMessageBlocks;

            return subscriptionsClient.PullAsync($"projects/{triggerAttribute.ProjectId}/subscriptions/{triggerAttribute.SubscriptionId}",
                    new PullRequest() {
                        maxMessages = maxMessages,
                        returnImmediately = false
                    },
                    null,
                    cancellationToken)
                .ContinueWith(GetMessages);

        }

        private IEnumerable<(TriggeredFunctionData messages, IEnumerable<string> ackIds)> GetMessages(Task<BaseResponse<PullResponse>> pullTask) {

            var pull = pullTask.Result;

            if (pull != null && pull.Success && pull.Response.receivedMessages != null && pull.Response.receivedMessages.Count() > 0) {

                for (int i = 0; i < NumberOfMessageBlocks; i++) {

                    var messagesBlock = pull.Response.receivedMessages.Skip(i * triggerAttribute.MaxBatchSize).Take(triggerAttribute.MaxBatchSize);

                    //IEnumerable<string> messages = messagesBlock.Select(c => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(c.message.data)));
                    IEnumerable<string> ackIds = messagesBlock.Select(c => c.ackId);

                    TriggeredFunctionData input = new TriggeredFunctionData {
                        TriggerValue = messagesBlock.Select(c => c.message)
                    };

                    yield return (input, ackIds);
                }
            } else {
                if (pull != null && (!pull.Success)) {
                    throw new Exception(pull.ErrorText);
                }
            }

        }

    }
}