using Microsoft.Azure.WebJobs.Description;
using System;

namespace AzureFunctions.Extensions.GooglePubSub {

    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class GooglePubSubAttribute : Attribute {

        /// <summary>
        /// Attribute to write to Google PubSub. Works with type 'ICollector<string>'
        /// </summary>
        /// <param name="credentialsFileName">
        /// the file "credencials.json" should be the Service Account file downloaded from the Google Cloud Platform website and located in the base executable folder of the functions project. 
        /// ( just add the file to your project and mark it to "copy always" )
        /// If you leave it empty -> "", the default credentials should be configured at machine level.
        /// IMPORTANT: If you make it null, the function fail to run.
        /// </param>
        /// <param name="projectId">projectId inside google cloud</param>
        /// <param name="topicId">PubSub topicId to write to</param>
        public GooglePubSubAttribute(string credentialsFileName, string projectId, string topicId) {
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }

            CredentialsFileName = credentialsFileName ?? throw new ArgumentNullException(nameof(credentialsFileName));
            ProjectId = projectId;
            TopicId = topicId;
        }

        internal GooglePubSubAttribute(byte[] credentials, string projectId, string topicId) {
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }

            Credentials = credentials;
            ProjectId = projectId;
            TopicId = topicId;
        }

        internal GooglePubSubAttribute(string projectId, string topicId) {
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }

            ProjectId = projectId;
            TopicId = topicId;
        }

        /// <summary>
        /// using this contructor, the settings will come from the configuration file.
        /// you should configure:
        /// 'your configuration node name'.Credentials -> string representation of the JSON credential files given in the google cloud "service account" bit
        /// 'your configuration node name'.ProjectId -> projectId where the refered google pubsub is contained in
        /// 'your configuration node name'.TopicId -> topicId of the refered google pubsub 
        /// </summary>
        /// <param name="configurationNodeName">prefix name that you gave to your configuration.</param>
        public GooglePubSubAttribute(string configurationNodeName) {
            if (string.IsNullOrWhiteSpace(configurationNodeName)) { throw new ArgumentNullException(nameof(configurationNodeName)); }

            ConfigurationNodeName = configurationNodeName;
        }

        public string CredentialsFileName { get; }
        internal byte[] Credentials { get; }

        public string ProjectId { get; }
        public string TopicId { get; }
        public string ConfigurationNodeName { get; }

        public int AcknowledgeDeadline { get; set; } = 600;

        internal static GooglePubSubAttribute GetAttributeByConfiguration(GooglePubSubAttribute googlePubSubAttribute) {
            if (string.IsNullOrWhiteSpace(googlePubSubAttribute.ConfigurationNodeName)) { return googlePubSubAttribute; }

            var credentialsString = System.Environment.GetEnvironmentVariable($"{googlePubSubAttribute.ConfigurationNodeName}.Credentials", System.EnvironmentVariableTarget.Process);
            var credentialsFileName = System.Environment.GetEnvironmentVariable($"{googlePubSubAttribute.ConfigurationNodeName}.CredentialsFileName", System.EnvironmentVariableTarget.Process);
            var projectId = System.Environment.GetEnvironmentVariable($"{googlePubSubAttribute.ConfigurationNodeName}.ProjectId", System.EnvironmentVariableTarget.Process);
            var topicId = System.Environment.GetEnvironmentVariable($"{googlePubSubAttribute.ConfigurationNodeName}.TopicId", System.EnvironmentVariableTarget.Process);

            if (string.IsNullOrWhiteSpace(credentialsString) && string.IsNullOrEmpty(credentialsFileName)) {
                return new GooglePubSubAttribute(projectId, topicId);
            } else {
                if (string.IsNullOrWhiteSpace(credentialsString)) {
                    return new GooglePubSubAttribute(credentialsFileName, projectId, topicId);
                } else {
                    var credentials = System.Text.Encoding.UTF8.GetBytes(credentialsString);
                    return new GooglePubSubAttribute(credentials, projectId, topicId);
                }
            }

        }

    }
}