using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Google.Cloud.PubSub.V1;
using System;
using System.Linq;
using System.Collections.Generic;

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
                SubscriberClient subscriber = CreatorService.GetSubscriberClient(triggerAttribute);

                TopicName topicName = new TopicName(projectId, topicId);

                Subscription subscription = null;
                try
                {
                    subscription = await subscriber.GetSubscriptionAsync(subscriptionName, cancellationToken);
                    await subscriber.ModifyAckDeadlineAsync(new ModifyAckDeadlineRequest() { AckDeadlineSeconds = triggerAttribute.AcknowledgeDeadline, SubscriptionAsSubscriptionName = subscriptionName });
                }
                catch (Exception) { }

                if (subscription == null && triggerAttribute.CreateSubscriptionIfDoesntExist)
                {
                    subscription = await subscriber.CreateSubscriptionAsync(subscriptionName, topicName, null, triggerAttribute.AcknowledgeDeadline, cancellationToken);
                }
            }

            //TODO: clean this mess
            var t1 = StartLister(subscriptionName, cancellationToken);
            var t2 = StartLister(subscriptionName, cancellationToken);
            var t3 = StartLister(subscriptionName, cancellationToken);
            var t4 = StartLister(subscriptionName, cancellationToken);
            var t5 = StartLister(subscriptionName, cancellationToken);
            var t6 = StartLister(subscriptionName, cancellationToken);
            var t7 = StartLister(subscriptionName, cancellationToken);
            var t8 = StartLister(subscriptionName, cancellationToken);
            var t9 = StartLister(subscriptionName, cancellationToken);
            var t10 = StartLister(subscriptionName, cancellationToken);

            await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

        }

        private async Task StartLister(SubscriptionName subscriptionName, CancellationToken cancellationToken)
        {

            //Seems that the subscriber loses performance through out the time.
            //So, it will be replaced every 10 min.
            SubscriberClient subscriber = CreatorService.GetSubscriberClient(triggerAttribute);

            var t1 = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {

                if ((DateTime.UtcNow - t1).TotalMinutes > 10)
                {
                    subscriber = CreatorService.GetSubscriberClient(triggerAttribute);
                    t1 = DateTime.UtcNow;
                }

                PullResponse pullResponse = null;
                try
                {
                    pullResponse = await subscriber.PullAsync(subscriptionName, false, triggerAttribute.MaxBatchSize, cancellationToken);
                }
                catch (Exception) { }

                if (pullResponse != null && pullResponse.ReceivedMessages != null && pullResponse.ReceivedMessages.Count > 0)
                {

                    IEnumerable<string> messages = pullResponse.ReceivedMessages.Select(c => c.Message.Data.ToStringUtf8());
                    var ackIds = pullResponse.ReceivedMessages.Select(c => c.AckId);

                    TriggeredFunctionData input = new TriggeredFunctionData
                    {
                        TriggerValue = messages
                    };

                    await executor.TryExecuteAsync(input, cancellationToken)
                        .ContinueWith(async (functionResultTask) =>
                        {

                            FunctionResult functionResult = functionResultTask.Result;
                            if (functionResult.Succeeded)
                            {
                                await subscriber.AcknowledgeAsync(subscriptionName, ackIds, cancellationToken);
                            }

                        }, cancellationToken);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

    }
}