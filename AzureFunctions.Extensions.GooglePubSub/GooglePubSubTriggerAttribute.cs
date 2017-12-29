using Microsoft.Azure.WebJobs.Description;
using System;

namespace AzureFunctions.Extensions.GooglePubSub {

    [Binding]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class GooglePubSubTriggerAttribute : Attribute {

        private readonly string projectId;
        private readonly string topicId;
        private readonly string subscriptionId;
        private readonly string credentialsFileName;

        public GooglePubSubTriggerAttribute(string credentialsFileName, string projectId, string topicId, string subscriptionId) {
            if (string.IsNullOrWhiteSpace(credentialsFileName)) { throw new ArgumentNullException(nameof(credentialsFileName)); }
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }
            if (string.IsNullOrWhiteSpace(subscriptionId)) { throw new ArgumentNullException(nameof(subscriptionId)); }

            this.projectId = projectId;
            this.topicId = topicId;
            this.subscriptionId = subscriptionId;
            this.credentialsFileName = credentialsFileName;
        }

        public string ProjectId => projectId;
        public string TopicId => topicId;
        public string SubscriptionId => subscriptionId;
        public string CredentialsFileName => credentialsFileName;
        public bool CreateSubscriptionIfDoesntExist { get; set; } = true;
        public int MaxBatchSize { get; set; } = 100;

    }
}
