using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using System;
using System.Linq;
using System.Collections.Generic;
using AzureFunctions.Extensions.GooglePubSub.PubSub;

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
            PubSub.SubscriberClient subscriberClient = new PubSub.SubscriberClient(credentials, triggerAttribute.ProjectId, triggerAttribute.SubscriptionId);
            
            await CreateSubscription(subscriberClient, cancellationToken);

            var processorCount = Math.Max(2, Environment.ProcessorCount);

            var index = 0;
            Parallel.For(1, processorCount, async (t) => {
                while (!cancellationToken.IsCancellationRequested) {
                    await ListenerPull(subscriberClient, index, cancellationToken);
                }
            });

            //var listTasks = new List<Task>();
            //while (!cancellationToken.IsCancellationRequested) {
            //while (listTasks.Count() < processorCount) {
            //var item = ListenerPull(credentials, index++, cancellationToken);
            //listTasks.Add(item);
            //}

            //await Task.WhenAll(listTasks);
            //}

        }

        Task IListener.StopAsync(CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }

        private Task CreateSubscription(PubSub.SubscriberClient subscriberClient, CancellationToken cancellationToken) {
            
            var createSubscriptionRequest = new CreateSubscriptionRequest() {
                ackDeadlineSeconds = triggerAttribute.AcknowledgeDeadline,
                name = triggerAttribute.SubscriptionId,
                topic = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}"
            };
            
            return subscriberClient.CreateAsync(createSubscriptionRequest, cancellationToken);

        }

        private Task ListenerPull(PubSub.SubscriberClient subscriberClient, int index, CancellationToken cancellationToken) {

            return GetTypeInput(subscriberClient, cancellationToken)
                .ContinueWith((typeInputTask) => {
                    IEnumerable<(TriggeredFunctionData, IEnumerable<string>)> buckets = typeInputTask.Result;

                    if (buckets != null && buckets.Any()) {

                        var listExecutions = new List<Task>();
                        foreach ((TriggeredFunctionData input, IEnumerable<string> ackIds) in buckets) {
                            if (input != null && ackIds != null && ackIds.Any()) {

                                //System.Diagnostics.Debug.WriteLine($"{DateTime.Now:HH:mm:ss:fff} - {index} - received {ackIds.Count()} messages");

                                var t = executor.TryExecuteAsync(input, cancellationToken)
                                        .ContinueWith((functionResultTask) => {

                                            FunctionResult functionResult = functionResultTask.Result;
                                            if (functionResult.Succeeded) {
                                                return subscriberClient.AcknowledgeAsync(ackIds, cancellationToken);
                                            }

                                            return Task.CompletedTask;

                                        }, cancellationToken).Unwrap();

                                listExecutions.Add(t);

                            }
                        }

                        return Task.WhenAll(listExecutions)
                            .ContinueWith((listExecutionsTask) => {
                                if (listExecutionsTask.IsFaulted) {
                                    throw listExecutionsTask.Exception.InnerException;
                                }
                            });
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

    }
}