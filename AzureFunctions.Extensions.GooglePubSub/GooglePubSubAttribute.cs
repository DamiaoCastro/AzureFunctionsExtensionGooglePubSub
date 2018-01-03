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

        public string CredentialsFileName { get; }
        public string ProjectId { get; }
        public string TopicId { get; }
    }
}