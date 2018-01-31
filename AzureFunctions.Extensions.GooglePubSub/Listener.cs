using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Google.Cloud.PubSub.V1;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace AzureFunctions.Extensions.GooglePubSub
{
    internal class Listener : IListener
    {

        private ITriggeredFunctionExecutor executor;
        private readonly GooglePubSubTriggerAttribute triggerAttribute;

        public Listener(ITriggeredFunctionExecutor executor, GooglePubSubTriggerAttribute triggerAttribute)
        {
            this.executor = executor;
            this.triggerAttribute = GooglePubSubTriggerAttribute.GetAttributeByConfiguration(triggerAttribute);
        }

        public void Cancel()
        {
        }

        public void Dispose()
        {
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            string projectId = triggerAttribute.ProjectId;
            string topicId = triggerAttribute.TopicId;
            string subscriptionId = triggerAttribute.SubscriptionId;

            SubscriptionName subscriptionName = new SubscriptionName(projectId, subscriptionId);
            {
                Subscriber.SubscriberClient subscriber = CreatorService.GetSubscriberClient(triggerAttribute);

                TopicName topicName = new TopicName(projectId, topicId);

                Subscription subscription = null;
                try
                {
                    subscription = await subscriber.GetSubscriptionAsync(new GetSubscriptionRequest() { SubscriptionAsSubscriptionName = subscriptionName }, null, null, cancellationToken);
                    //await subscriber.ModifyAckDeadlineAsync(new ModifyAckDeadlineRequest() { AckDeadlineSeconds = triggerAttribute.AcknowledgeDeadline, SubscriptionAsSubscriptionName = subscriptionName });
                }
                catch (Exception) { }

                if (subscription == null && triggerAttribute.CreateSubscriptionIfDoesntExist)
                {
                    subscription = await subscriber.CreateSubscriptionAsync(
                        new Subscription()
                        {
                            AckDeadlineSeconds = triggerAttribute.AcknowledgeDeadline,
                            SubscriptionName = subscriptionName,
                            TopicAsTopicNameOneof = TopicNameOneof.From(topicName)
                        }, null, null, cancellationToken);
                }
            }

            //TODO: clean this mess
            var t1 = StartLister(cancellationToken);
            var t2 = StartLister(cancellationToken);
            var t3 = StartLister(cancellationToken);
            var t4 = StartLister(cancellationToken);
            var t5 = StartLister(cancellationToken);
            var t6 = StartLister(cancellationToken);
            var t7 = StartLister(cancellationToken);
            var t8 = StartLister(cancellationToken);
            var t9 = StartLister(cancellationToken);
            var t10 = StartLister(cancellationToken);

            await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        private async Task StartLister(CancellationToken cancellationToken)
        {

            var credentials = CreatorService.GetCredentials(triggerAttribute);
            PubSub.SubscriberClient subscriberClient = new PubSub.SubscriberClient(credentials, triggerAttribute.ProjectId, triggerAttribute.SubscriptionId);

            while (!cancellationToken.IsCancellationRequested)
            {
                var (input, ackIds) = await GetTypeInput(subscriberClient, cancellationToken);

                await executor.TryExecuteAsync(input, cancellationToken)
                        .ContinueWith(async (functionResultTask) =>
                        {

                            FunctionResult functionResult = functionResultTask.Result;
                            if (functionResult.Succeeded)
                            {
                                await subscriberClient.AcknowledgeAsync(ackIds, cancellationToken);
                            }

                        }, cancellationToken).Unwrap();
            }

            
        }

        private async Task<(TriggeredFunctionData messages, IEnumerable<string> ackIds)> GetTypeInput(SubscriptionName subscriptionName, Subscriber.SubscriberClient subscriber, CancellationToken cancellationToken)
        {

            var pullRequest = new PullRequest()
            {
                SubscriptionAsSubscriptionName = subscriptionName,
                ReturnImmediately = false,
                MaxMessages = triggerAttribute.MaxBatchSize
            };

            var pull = await subscriber.PullAsync(pullRequest, null, null, cancellationToken);

            ReceivedMessage[] receivedMessages = pull.ReceivedMessages?.ToArray();
            pull = null;

            if (receivedMessages != null && receivedMessages != null && receivedMessages.Count() > 0)
            {

                IEnumerable<string> messages = receivedMessages.Select(c => c.Message.Data.ToStringUtf8()).ToArray();
                IEnumerable<string> ackIds = receivedMessages.Select(c => c.AckId).ToArray();
                receivedMessages = null;

                TriggeredFunctionData input = new TriggeredFunctionData
                {
                    TriggerValue = messages
                };

                return (input, ackIds);
            }

            return ((TriggeredFunctionData)null, (IEnumerable<string>)null);
        }

        private async Task<(TriggeredFunctionData messages, IEnumerable<string> ackIds)> GetTypeInput(PubSub.SubscriberClient subscriberClient, CancellationToken cancellationToken)
        {

            var pull = await subscriberClient.PullAsync(triggerAttribute.MaxBatchSize, false, cancellationToken);

            if (pull != null && pull.receivedMessages != null && pull.receivedMessages.Count() > 0)
            {
                IEnumerable<string> messages = pull.receivedMessages.Select(c => c.message.dataString);
                IEnumerable<string> ackIds = pull.receivedMessages.Select(c => c.ackId);

                TriggeredFunctionData input = new TriggeredFunctionData
                {
                    TriggerValue = messages
                };

                return (input, ackIds);
            }

            return ((TriggeredFunctionData)null, (IEnumerable<string>)null);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

    }
}