using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Google.Cloud.PubSub.V1;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http.Headers;
using AzureFunctions.Extensions.GooglePubSub.PubSub;

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

            await CreateSubscription(cancellationToken);

            var credentials = CreatorService.GetCredentials(triggerAttribute);

            var listTasks = new List<Task>();
            var index = 0;

            var processorCount = Math.Max(2, Environment.ProcessorCount);

            while (!cancellationToken.IsCancellationRequested) {

                //while (listTasks.Count() < processorCount) {
                var item = ListenerPull(credentials, index++, cancellationToken);
                listTasks.Add(item);
                //}

                await Task.WhenAll(listTasks);
            }

        }

        private async Task CreateSubscription(CancellationToken cancellationToken) {

            SubscriptionName subscriptionName = new SubscriptionName(triggerAttribute.ProjectId, triggerAttribute.SubscriptionId);
            {
                Subscriber.SubscriberClient subscriber = CreatorService.GetSubscriberClient(triggerAttribute);

                TopicName topicName = new TopicName(triggerAttribute.ProjectId, triggerAttribute.TopicId);

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
                    IEnumerable<(TriggeredFunctionData, IEnumerable<string>)> buckets = typeInputTask.Result;

                    if (buckets != null && buckets.Any()) {
                        //var listExecutions = new List<Task>();

                        Parallel.ForEach(buckets, async (item) => {
                            (TriggeredFunctionData input, IEnumerable<string> ackIds) = item;

                            if (input != null && ackIds != null && ackIds.Any()) {
#if DEBUG
                                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss:fff} - {index} - received {ackIds.Count()} messages");
#endif
                                await executor.TryExecuteAsync(input, cancellationToken)
                                        .ContinueWith((functionResultTask) => {

                                            FunctionResult functionResult = functionResultTask.Result;
                                            if (functionResult.Succeeded) {
                                                return subscriberClient.AcknowledgeAsync(ackIds, cancellationToken);
                                            }

                                            return Task.CompletedTask;

                                        }, cancellationToken).Unwrap();

                            }
                        });

                        //                        foreach ((TriggeredFunctionData input, IEnumerable<string> ackIds) in buckets) {
                        //                            if (input != null && ackIds != null && ackIds.Any()) {
                        //#if DEBUG
                        //                                System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss:fff} - {index} - received {ackIds.Count()} messages");
                        //#endif
                        //                                var t = executor.TryExecuteAsync(input, cancellationToken)
                        //                                        .ContinueWith((functionResultTask) => {

                        //                                            FunctionResult functionResult = functionResultTask.Result;
                        //                                            if (functionResult.Succeeded) {
                        //                                                return subscriberClient.AcknowledgeAsync(ackIds, cancellationToken);
                        //                                            }

                        //                                            return Task.CompletedTask;

                        //                                        }, cancellationToken).Unwrap();

                        //                                listExecutions.Add(t);

                        //                            }
                        //                        }

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

        private async Task<IEnumerable<(TriggeredFunctionData messages, IEnumerable<string> ackIds)>> GetTypeInput(PubSub.SubscriberClient subscriberClient, CancellationToken cancellationToken) {

            SubscriberPullResponse pull = await subscriberClient.PullAsync(triggerAttribute.MaxBatchSize * 10, false, cancellationToken);

            if (pull != null && pull.receivedMessages != null && pull.receivedMessages.Count() > 0) {

                var list = new List<(TriggeredFunctionData messages, IEnumerable<string> ackIds)>();
                for (int i = 0; i < 10; i++) {

                    IEnumerable<string> messages = pull.receivedMessages.Skip(i * triggerAttribute.MaxBatchSize).Take(triggerAttribute.MaxBatchSize).Select(c => c.message.dataString);
                    if (!messages.Any()) { break; }
                    IEnumerable<string> ackIds = pull.receivedMessages.Skip(i * triggerAttribute.MaxBatchSize).Take(triggerAttribute.MaxBatchSize).Select(c => c.ackId);

                    TriggeredFunctionData input = new TriggeredFunctionData {
                        TriggerValue = messages
                    };

                    list.Add((input, ackIds));
                }

                return list;
            }

            return (IEnumerable<(TriggeredFunctionData messages, IEnumerable<string> ackIds)>)null;
        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }

    }
}