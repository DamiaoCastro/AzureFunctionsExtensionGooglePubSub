using Microsoft.Azure.WebJobs;
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

                PubSub.PublisherClient publisher = PublisherClientCache.GetPublisherClientV2(googlePubSubAttribute);

                var bulkSize = items.Count() / 1000;
                if (bulkSize > 0) {
                    var bulkTasks = new List<Task>();
                    for (var index = 0; index <= bulkSize; index++) {
                        bulkTasks.Add(publisher.PublishAsync(items.Skip(index * 1000).Take(1000), cancellationToken));
                    }
                    return Task.WhenAll(bulkTasks);
                } else {
                    return publisher.PublishAsync(items, cancellationToken);
                }

            }

            return Task.CompletedTask;
        }

    }
}