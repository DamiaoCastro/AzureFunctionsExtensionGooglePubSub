using Google.Cloud.PubSub.V1;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureFunctions.Extensions.GooglePubSub {

    internal class AsyncCollector : ICollector<string>, IAsyncCollector<string> {

        private readonly GooglePubSubAttribute googlePubSubAttribute;
        private List<string> items = new List<string>();

        public AsyncCollector(GooglePubSubAttribute googlePubSubAttribute) {
            this.googlePubSubAttribute = GooglePubSubAttribute.GetAttributeByConfiguration(googlePubSubAttribute);
        }

        void ICollector<string>.Add(string item) {
            items.Add(item);
        }

        Task IAsyncCollector<string>.AddAsync(string item, CancellationToken cancellationToken) {
            items.Add(item);
            return Task.WhenAll();
        }

        Task IAsyncCollector<string>.FlushAsync(CancellationToken cancellationToken) {

            if (items.Any()) {
                Publisher.PublisherClient publisher = PublisherClientCache.GetPublisherClient(googlePubSubAttribute);

                var topicName = new TopicName(googlePubSubAttribute.ProjectId, googlePubSubAttribute.TopicId);

                var bulkSize = items.Count() / 1000;
                var bulkTasks = new List<Task>();
                for (var index = 0; index <= bulkSize; index++) {

                    var pubSubMessages = items.Skip(index * 1000).Take(1000).Select(c => new PubsubMessage() { Data = Google.Protobuf.ByteString.CopyFromUtf8(c) });
                    var publishRequest = new PublishRequest() { TopicAsTopicName = topicName };
                    publishRequest.Messages.AddRange(pubSubMessages);

                    bulkTasks.Add(publisher.PublishAsync(publishRequest, null, null, cancellationToken).ResponseAsync);
                }

                return Task.WhenAll(bulkTasks);
            }

            return Task.CompletedTask;
        }

    }
}