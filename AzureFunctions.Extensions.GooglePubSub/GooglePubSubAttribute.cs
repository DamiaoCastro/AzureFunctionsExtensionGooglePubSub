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
        /// If you leave it null, the default credentials should be configured at machine level
        /// </param>
        /// <param name="projectId">projectId inside google cloud</param>
        /// <param name="topicId">PubSub topicId to write to</param>
        public GooglePubSubAttribute(string credentialsFileName, string projectId, string topicId) {
            //if (string.IsNullOrWhiteSpace(credentialsFileName)) { throw new ArgumentNullException(nameof(credentialsFileName)); }
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }

            CredentialsFileName = credentialsFileName;
            ProjectId = projectId;
            TopicId = topicId;
        }

        public GooglePubSubAttribute(string projectId, string topicId) {
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }
            if (string.IsNullOrWhiteSpace(topicId)) { throw new ArgumentNullException(nameof(topicId)); }

            CredentialsFileName = string.Empty;
            ProjectId = projectId;
            TopicId = topicId;
        }

        public string CredentialsFileName { get; }
        public string ProjectId { get; }
        public string TopicId { get; }
    }
}