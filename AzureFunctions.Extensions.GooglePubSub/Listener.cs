using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Google.Cloud.PubSub.V1;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace AzureFunctions.Extensions.GooglePubSub {
    internal class Listener : IListener {

        private ITriggeredFunctionExecutor executor;
        private readonly GooglePubSubTriggerAttribute triggerAttribute;

        public Listener(ITriggeredFunctionExecutor executor, GooglePubSubTriggerAttribute triggerAttribute) {
            this.executor = executor;
            this.triggerAttribute = GooglePubSubTriggerAttribute.GetAttributeByConfiguration(triggerAttribute);
        }

        public void Cancel() {
        }

        public void Dispose() {
        }

        public async Task StartAsync(CancellationToken cancellationToken) {

            string projectId = triggerAttribute.ProjectId;
            string topicId = triggerAttribute.TopicId;
            string subscriptionId = triggerAttribute.SubscriptionId;

            SubscriptionName subscriptionName = new SubscriptionName(projectId, subscriptionId);
            {
                Subscriber.SubscriberClient subscriber = CreatorService.GetSubscriberClient(triggerAttribute);

                TopicName topicName = new TopicName(projectId, topicId);

                Subscription subscription = null;
                try {
                    subscription = await subscriber.GetSubscriptionAsync(new GetSubscriptionRequest() { SubscriptionAsSubscriptionName = subscriptionName }, null, null, cancellationToken);
                } catch (Exception) { }

                if (subscription == null && triggerAttribute.CreateSubscriptionIfDoesntExist) {
                    subscription = await subscriber.CreateSubscriptionAsync(
                        new Subscription() {
                            AckDeadlineSeconds = triggerAttribute.AcknowledgeDeadline,
                            SubscriptionName = subscriptionName,
                            TopicAsTopicNameOneof = TopicNameOneof.From(topicName)
                        }, null, null, cancellationToken);
                }
            }

            var credentials = CreatorService.GetCredentials(triggerAttribute);

            var listTasks = new List<Task>();
            var index = 0;

            var processorCount = Math.Max(2, Environment.ProcessorCount);

            while (!cancellationToken.IsCancellationRequested) {

                while (listTasks.Count() < processorCount) {
                    var item = ListenerPull(credentials, index++, cancellationToken);
                    listTasks.Add(item);
                }

                await Task.WhenAll(listTasks);
            }

        }
        private async Task ListenerPull(byte[] credentials, int index, CancellationToken cancellationToken) {

            PubSub.SubscriberClient subscriberClient = new PubSub.SubscriberClient(credentials, triggerAttribute.ProjectId, triggerAttribute.SubscriptionId);
            while (!cancellationToken.IsCancellationRequested) {
                await ListenerPull(subscriberClient, index, cancellationToken);
            }
        }

        private Task ListenerPull(PubSub.SubscriberClient subscriberClient, int index, CancellationToken cancellationToken) {

            return GetTypeInput(subscriberClient, cancellationToken)
                .ContinueWith((typeInputTask) => {
                    (TriggeredFunctionData input, IEnumerable<string> ackIds) = typeInputTask.Result;

                    if (input != null && ackIds != null) {

//#if DEBUG
//                        System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss:fff} - {index} - received {ackIds.Count()} messages");
//#endif
                        return executor.TryExecuteAsync(input, cancellationToken)
                                .ContinueWith((functionResultTask) => {

                                    FunctionResult functionResult = functionResultTask.Result;
                                    if (functionResult.Succeeded) {
                                        return subscriberClient.AcknowledgeAsync(ackIds, cancellationToken);
                                    }

                                    return Task.CompletedTask;

                                }, cancellationToken).Unwrap();

                    }

                    return Task.CompletedTask;

                }).Unwrap();

        }

        private Task<(TriggeredFunctionData messages, IEnumerable<string> ackIds)> GetTypeInput(PubSub.SubscriberClient subscriberClient, CancellationToken cancellationToken) {

            return subscriberClient.PullAsync(triggerAttribute.MaxBatchSize, false, cancellationToken)
                .ContinueWith((pullTask) => {

                    var pull = pullTask.Result;

                    if (pull != null && pull.receivedMessages != null && pull.receivedMessages.Count() > 0) {
                        IEnumerable<string> messages = pull.receivedMessages.Select(c => c.message.dataString);
                        IEnumerable<string> ackIds = pull.receivedMessages.Select(c => c.ackId);

                        TriggeredFunctionData input = new TriggeredFunctionData {
                            TriggerValue = messages
                        };

                        return (input, ackIds);
                    }

                    return ((TriggeredFunctionData)null, (IEnumerable<string>)null);
                });

        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }

    }
}