using Microsoft.Azure.WebJobs.Description;
using System;

namespace AzureFunctions.Extensions.GooglePubSub {

    [Binding]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class GooglePubSubTriggerAttribute : Attribute {

        /// <summary>
        /// Attribute to read from Google PubSub. Works with type 'IEnumerable<string>' and 'string[]'
        /// </summary>
        /// <param name="credentialsFileName">the file "credencials.json" should be the Service Account file downloaded from the Google Cloud Platform website and located in the base executable folder of the functions project. 
        /// ( just add the file to your project and mark it to "copy always" )
        /// If you leave it empty -> "", the default credentials should be configured at machine level.
        /// IMPORTANT: If you make it null, the function fail to run.</param>
        /// <param name="projectId">projectId inside google cloud</param>
        /// <param name="topicId">PubSub topicId to read from</param>
        /// <param name="subscriptionId">subscriptionId to use</param>
        public GooglePubSubTriggerAttribute(string credentialsFileName, string projectId, string topicId, string subscriptionId) {
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }
            if (string.IsNullOrWhiteSpace(subscriptionId)) { throw new ArgumentNullException(nameof(subscriptionId)); }

            CredentialsFileName = credentialsFileName ?? throw new ArgumentNullException(nameof(credentialsFileName));
            ProjectId = projectId;
            TopicId = topicId;
            SubscriptionId = subscriptionId;
        }

        internal GooglePubSubTriggerAttribute(byte[] credentials, string projectId, string topicId, string subscriptionId) {
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }
            if (string.IsNullOrWhiteSpace(subscriptionId)) { throw new ArgumentNullException(nameof(subscriptionId)); }

            Credentials = credentials;
            ProjectId = projectId;
            TopicId = topicId;
            SubscriptionId = subscriptionId;
        }

        public GooglePubSubTriggerAttribute(string configurationNodeName) {
            if (string.IsNullOrWhiteSpace(configurationNodeName)) { throw new ArgumentNullException(nameof(configurationNodeName)); }

            ConfigurationNodeName = configurationNodeName;
        }

        public string CredentialsFileName { get; }
        internal byte[] Credentials { get; }

        public string ProjectId { get; }
        public string TopicId { get; }
        public string SubscriptionId { get; }
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

        public string ConfigurationNodeName { get; }

        internal static GooglePubSubTriggerAttribute GetAttributeByConfiguration(GooglePubSubTriggerAttribute googlePubSubTriggerAttribute) {
            if (string.IsNullOrWhiteSpace(googlePubSubTriggerAttribute.ConfigurationNodeName)) { return googlePubSubTriggerAttribute; }

            var credentialsString = Environment.GetEnvironmentVariable($"{googlePubSubTriggerAttribute.ConfigurationNodeName}.{nameof(Credentials)}", EnvironmentVariableTarget.Process);
            var credentialsFileName = Environment.GetEnvironmentVariable($"{googlePubSubTriggerAttribute.ConfigurationNodeName}.{nameof(CredentialsFileName)}", EnvironmentVariableTarget.Process);
            var projectId = Environment.GetEnvironmentVariable($"{googlePubSubTriggerAttribute.ConfigurationNodeName}.{nameof(ProjectId)}", EnvironmentVariableTarget.Process);
            var topicId = Environment.GetEnvironmentVariable($"{googlePubSubTriggerAttribute.ConfigurationNodeName}.{nameof(TopicId)}", EnvironmentVariableTarget.Process);
            var subscriptionId = Environment.GetEnvironmentVariable($"{googlePubSubTriggerAttribute.ConfigurationNodeName}.{nameof(SubscriptionId)}", EnvironmentVariableTarget.Process);

            GooglePubSubTriggerAttribute newGooglePubSubTriggerAttribute = null;
            if (string.IsNullOrWhiteSpace(credentialsString)) {
                newGooglePubSubTriggerAttribute = new GooglePubSubTriggerAttribute(credentialsFileName, projectId, topicId, subscriptionId);
            } else {
                var credentials = System.Text.Encoding.UTF8.GetBytes(credentialsString);
                newGooglePubSubTriggerAttribute = new GooglePubSubTriggerAttribute(credentials, projectId, topicId, subscriptionId);
            }

            var createSubscriptionIfDoesntExist = Environment.GetEnvironmentVariable($"{googlePubSubTriggerAttribute.ConfigurationNodeName}.{nameof(CreateSubscriptionIfDoesntExist)}", EnvironmentVariableTarget.Process);
            var maxBatchSize = Environment.GetEnvironmentVariable($"{googlePubSubTriggerAttribute.ConfigurationNodeName}.{nameof(MaxBatchSize)}", EnvironmentVariableTarget.Process);

            if (createSubscriptionIfDoesntExist != null) {
                newGooglePubSubTriggerAttribute.CreateSubscriptionIfDoesntExist = bool.Parse(createSubscriptionIfDoesntExist);
            }

            if (maxBatchSize != null && System.Text.RegularExpressions.Regex.IsMatch(maxBatchSize, "^\\d+$")) {
                newGooglePubSubTriggerAttribute.MaxBatchSize = int.Parse(maxBatchSize);
            }

            return newGooglePubSubTriggerAttribute;
        }

    }
}
