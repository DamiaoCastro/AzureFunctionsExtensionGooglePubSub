using Google.Cloud.PubSub.V1;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AzureFunctions.Extensions.GooglePubSub
{
    public static class PublisherClientCache
    {
        
        public static ConcurrentDictionary<int, PublisherClient> publisherClientCache = new ConcurrentDictionary<int, PublisherClient>();

        public static PublisherClient GetPublisherClient(GooglePubSubAttribute googlePubSubAttribute)
        {
            var key = googlePubSubAttribute.GetHashCode();
            if (!publisherClientCache.ContainsKey(key))
            {
                var publisherClient = CreatorService.GetPublisherClient(googlePubSubAttribute);
                publisherClientCache.AddOrUpdate(key, publisherClient, (newkey, oldValue) => publisherClient);
            }
            return publisherClientCache[key];
        }

    }
}
