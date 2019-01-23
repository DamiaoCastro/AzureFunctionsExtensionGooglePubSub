using Microsoft.Azure.WebJobs.Description;
using System;

namespace AzureFunctions.Extensions.GooglePubSub {

    [Binding]
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class GooglePubSubTriggerAttribute : GooglePubSubBaseAttribute {

        /// <summary>
        /// Attribute to read from Google PubSub. Works with type 'IEnumerable<string>' and 'string[]'
        /// </summary>
        /// <param name="credentialsSettingKey">setting key where the value is the string representation of the JSON credential files given in the google cloud service account.</param>
        /// <param name="projectId">projectId inside google cloud</param>
        /// <param name="topicId">PubSub topicId to read from</param>
        /// <param name="subscriptionId">subscriptionId to use</param>
        public GooglePubSubTriggerAttribute(string credentialsSettingKey, string projectId, string topicId, string subscriptionId) : base(credentialsSettingKey) {
            ProjectId = projectId;
            TopicId = topicId;
            SubscriptionId = subscriptionId;
        }

        /// <summary>
        /// using this contructor, the settings will come from the settings file.
        /// you should configure:
        /// 'your configuration node name'.Credentials -> string representation of the JSON credential files given in the google cloud "service account" bit
        /// 'your configuration node name'.ProjectId -> projectId where the refered google pubsub is contained in
        /// 'your configuration node name'.TopicId -> topicId of the refered google pubsub 
        /// 'your configuration node name'.SubscriptionId -> subscriptionId that this function will use
        /// 'your configuration node name'.CreateSubscriptionIfDoesntExist -> bool to define if the subscription with the Id above should be created if doesn't exist
        /// 'your configuration node name'.MaxBatchSize -> max number of messages to receive
        /// </summary>
        /// <param name="configurationNodeName">prefix name that you gave to your configuration.</param>
        public GooglePubSubTriggerAttribute(string configurationNodeName) : base($"{configurationNodeName}.{nameof(Credentials)}") {

            ConfigurationNodeName = configurationNodeName;
            if (!string.IsNullOrWhiteSpace(ConfigurationNodeName)) {
                var projectId = Environment.GetEnvironmentVariable($"{configurationNodeName}.{nameof(ProjectId)}", EnvironmentVariableTarget.Process);
                var topicId = Environment.GetEnvironmentVariable($"{configurationNodeName}.{nameof(TopicId)}", EnvironmentVariableTarget.Process);
                var subscriptionId = Environment.GetEnvironmentVariable($"{configurationNodeName}.{nameof(SubscriptionId)}", EnvironmentVariableTarget.Process);

                ProjectId = projectId;
                TopicId = topicId;
                SubscriptionId = subscriptionId;
            }
        }

        public string ConfigurationNodeName { get; }

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

        public int AcknowledgeDeadline { get; set; } = 600;

        ///// <summary>
        ///// Number of parallel listeners that should be created for the trigger.
        ///// </summary>
        //public int NrListeners { get; internal set; } = 1;

        internal override string GetObjectKey() {
            return $"{ConfigurationNodeName}|{CredentialsSettingKey}|{ProjectId}|{TopicId}|{SubscriptionId}|{AcknowledgeDeadline}";
        }

    }
}