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

        /// <summary>
        /// Attribute to read from Google PubSub. Works with type 'IEnumerable<string>' and 'string[]'
        /// </summary>
        /// <param name="credentialsFileName">the file "credencials.json" should be the Service Account file downloaded from the Google Cloud Platform website and located in the base executable folder of the functions project. 
        /// ( just add the file to your project and mark it to "copy always" )
        /// If you leave it null, the default credentials should be configured at machine level</param>
        /// <param name="projectId">projectId inside google cloud</param>
        /// <param name="topicId">PubSub topicId to read from</param>
        /// <param name="subscriptionId">subscriptionId to use</param>
        public GooglePubSubTriggerAttribute(string credentialsFileName, string projectId, string topicId, string subscriptionId) {
            //if (string.IsNullOrWhiteSpace(credentialsFileName)) { throw new ArgumentNullException(nameof(credentialsFileName)); }
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }
            if (string.IsNullOrWhiteSpace(subscriptionId)) { throw new ArgumentNullException(nameof(subscriptionId)); }

            this.credentialsFileName = credentialsFileName;
            this.projectId = projectId;
            this.topicId = topicId;
            this.subscriptionId = subscriptionId;
        }

        public GooglePubSubTriggerAttribute(string projectId, string topicId, string subscriptionId) {
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }
            if (string.IsNullOrWhiteSpace(subscriptionId)) { throw new ArgumentNullException(nameof(subscriptionId)); }

            this.credentialsFileName = string.Empty;
            this.projectId = projectId;
            this.topicId = topicId;
            this.subscriptionId = subscriptionId;
        }

        public string ProjectId => projectId;
        public string TopicId => topicId;
        public string SubscriptionId => subscriptionId;
        public string CredentialsFileName => credentialsFileName;
        /// <summary>
        /// In case that there's no subscription to the given topic with the given subscriptionId, should it be created?
        /// Default true
        /// </summary>
        public bool CreateSubscriptionIfDoesntExist { get; set; } = true;
        /// <summary>
        /// Max number of messages to retrieve
        /// Default 100
        /// </summary>
        public int MaxBatchSize { get; set; } = 100;

    }
}
