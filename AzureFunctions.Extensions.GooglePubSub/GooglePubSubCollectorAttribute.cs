using Microsoft.Azure.WebJobs.Description;
using System;
using System.Linq;

namespace AzureFunctions.Extensions.GooglePubSub {

    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class GooglePubSubCollectorAttribute : GooglePubSubBaseAttribute {

        /// <summary>
        /// using this contructor, the settings will come from the settings file.
        /// you should configure:
        /// 'your configuration node name'.Credentials -> string representation of the JSON credential files given in the google cloud "service account" bit
        /// 'your configuration node name'.ProjectId -> projectId where the refered google pubsub is contained in
        /// 'your configuration node name'.TopicId -> topicId of the refered google pubsub 
        /// 
        /// Works with type 'ICollector<string>' and 'ICollector<PubsubMessage>'
        /// </summary>
        /// <param name="settingsNodeName">prefix name that you gave to your settings.</param>
        public GooglePubSubCollectorAttribute(string settingsNodeName) : base($"{settingsNodeName}.{nameof(Credentials)}") {

            ConfigurationNodeName = settingsNodeName;
            if (!string.IsNullOrWhiteSpace(ConfigurationNodeName)) {
                var projectId = Environment.GetEnvironmentVariable($"{settingsNodeName}.{nameof(ProjectId)}", EnvironmentVariableTarget.Process);
                var topicId = Environment.GetEnvironmentVariable($"{settingsNodeName}.{nameof(TopicId)}", EnvironmentVariableTarget.Process);

                ProjectId = projectId;
                TopicId = topicId;
            }
        }

        public GooglePubSubCollectorAttribute(string credentialsSettingKey, string fullNameSettingKey) : base(credentialsSettingKey) {

            if (!string.IsNullOrWhiteSpace(fullNameSettingKey)) {

                var value = Environment.GetEnvironmentVariable(fullNameSettingKey, EnvironmentVariableTarget.Process);
                if (!string.IsNullOrWhiteSpace(value)) {
                    var items = value.Split(new string[] { ":", "." }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Count() >= 2) {
                        ProjectId = items[0];
                        TopicId = items[1];
                    }
                }
            }

            FullNameSettingKey = fullNameSettingKey;
        }

        /// <summary>
        /// Attribute to write to Google PubSub. Works with type 'ICollector<string>' and 'ICollector<PubsubMessage>'
        /// </summary>
        /// <param name="credentialsSettingKey">setting key where the value is the string representation of the JSON credential files given in the google cloud service account.</param>
        /// <param name="projectId">projectId inside google cloud</param>
        /// <param name="topicId">PubSub topicId to write to</param>
        public GooglePubSubCollectorAttribute(string credentialsSettingKey, string projectId, string topicId) : base(credentialsSettingKey) {
            ProjectId = projectId;
            TopicId = topicId;
        }

        public string ConfigurationNodeName { get; }

        public string ProjectId { get; }

        public string TopicId { get; }

        public int AcknowledgeDeadline { get; set; } = 600;

        internal override string GetObjectKey() {
            return $"{ConfigurationNodeName}|{CredentialsSettingKey}|{ProjectId}|{TopicId}|{AcknowledgeDeadline}";
        }

    }
}