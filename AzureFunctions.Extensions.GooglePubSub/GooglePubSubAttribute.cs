using Microsoft.Azure.WebJobs.Description;
using System;

namespace AzureFunctions.Extensions.GooglePubSub {

    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class GooglePubSubAttribute : Attribute {
        public GooglePubSubAttribute(string credentialsFileName, string projectId, string topicId) {
            if (string.IsNullOrWhiteSpace(credentialsFileName)) { throw new ArgumentNullException(nameof(credentialsFileName)); }
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }

            CredentialsFileName = credentialsFileName;
            ProjectId = projectId;
            TopicId = topicId;
        }

        public string CredentialsFileName { get; }
        public string ProjectId { get; }
        public string TopicId { get; }
    }
}