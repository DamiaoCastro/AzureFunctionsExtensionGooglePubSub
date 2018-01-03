using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Google.Cloud.PubSub.V1;
using System;
using System.Linq;
using System.Collections.Generic;
using Grpc.Auth;

namespace AzureFunctions.Extensions.GooglePubSub {
    internal class Listener : IListener {

        private ITriggeredFunctionExecutor executor;
        private readonly GooglePubSubTriggerAttribute triggerAttribute;

        public Listener(ITriggeredFunctionExecutor executor, GooglePubSubTriggerAttribute triggerAttribute) {
            this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
            this.triggerAttribute = triggerAttribute ?? throw new ArgumentNullException(nameof(triggerAttribute));
        }

        public void Cancel() {
        }

        public void Dispose() {
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            
            SubscriberClient subscriber = CreatorService.GetSubscriberClient(triggerAttribute.CredentialsFileName);

            string projectId = triggerAttribute.ProjectId;
            string topicId = triggerAttribute.TopicId;
            string subscriptionId = triggerAttribute.SubscriptionId;

            TopicName topicName = new TopicName(projectId, topicId);
            SubscriptionName subscriptionName = new SubscriptionName(projectId, subscriptionId);
            int ackDeadlineSeconds = 10;

            Subscription subscription = null;
            try {
                subscription = await subscriber.GetSubscriptionAsync(subscriptionName, cancellationToken);
            } catch (Exception) { }

            if (subscription == null && triggerAttribute.CreateSubscriptionIfDoesntExist) {
                subscription = await subscriber.CreateSubscriptionAsync(subscriptionName, topicName, null, ackDeadlineSeconds, cancellationToken);
            }

            while (!cancellationToken.IsCancellationRequested) {

                PullResponse pullResponse = null;
                try {
                    pullResponse = await subscriber.PullAsync(subscriptionName, false, triggerAttribute.MaxBatchSize, cancellationToken);
                } catch (Exception) { }

                if (pullResponse != null && pullResponse.ReceivedMessages != null && pullResponse.ReceivedMessages.Count > 0) {

                    IEnumerable<string> messages = pullResponse.ReceivedMessages.Select(c => c.Message.Data.ToStringUtf8()).ToArray();
                    var ackIds = pullResponse.ReceivedMessages.Select(c => c.AckId);

                    TriggeredFunctionData input = new TriggeredFunctionData {
                        TriggerValue = messages
                    };

                    await executor.TryExecuteAsync(input, cancellationToken)
                        .ContinueWith(async (functionResultTask) => {

                            FunctionResult functionResult = functionResultTask.Result;
                            if (functionResult.Succeeded) {
                                await subscriber.AcknowledgeAsync(subscriptionName, ackIds);
                            }

                        }, cancellationToken);
                }

            }

        }

        public Task StopAsync(CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }

    }
}