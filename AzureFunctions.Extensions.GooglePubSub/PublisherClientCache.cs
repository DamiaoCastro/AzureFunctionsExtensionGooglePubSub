using Google.Cloud.PubSub.V1;
using System;
using System.Collections.Concurrent;

namespace AzureFunctions.Extensions.GooglePubSub
{
    public static class PublisherClientCache
    {
        
        private static ConcurrentDictionary<int, ExpiringPublisherClient> publisherClientCache = new ConcurrentDictionary<int, ExpiringPublisherClient>();

        public static Publisher.PublisherClient GetPublisherClient(GooglePubSubAttribute googlePubSubAttribute)
        {
            var key = googlePubSubAttribute.GetHashCode();

            if (publisherClientCache.ContainsKey(key))
            {
                var expiringBigQueryService = publisherClientCache[key];
                if ((DateTime.UtcNow - expiringBigQueryService.CreatedUtc).TotalMinutes > 9)
                {
                    var bigQueryService = CreatorService.GetPublisherClient(googlePubSubAttribute);
                    var expiringPublisherClient1 = new ExpiringPublisherClient(DateTime.UtcNow, bigQueryService);
                    publisherClientCache.AddOrUpdate(key, expiringPublisherClient1, (newkey, oldValue) => expiringPublisherClient1);

                    return bigQueryService;
                }

                return expiringBigQueryService.PublisherClient;
            }
            else
            {
                var bigQueryService = CreatorService.GetPublisherClient(googlePubSubAttribute);
                var expiringPublisherClient = new ExpiringPublisherClient(DateTime.UtcNow, bigQueryService);
                publisherClientCache.AddOrUpdate(key, expiringPublisherClient, (newkey, oldValue) => expiringPublisherClient);

                return bigQueryService;
            }
        }

        private class ExpiringPublisherClient
        {

            public ExpiringPublisherClient(DateTime createdUtc, Publisher.PublisherClient publisherClient)
            {
                CreatedUtc = createdUtc;
                PublisherClient = publisherClient;
            }

            public DateTime CreatedUtc { get; }
            public Publisher.PublisherClient PublisherClient { get; }
        }

    }
}
