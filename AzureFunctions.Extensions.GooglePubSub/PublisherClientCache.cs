using System.Collections.Concurrent;

namespace AzureFunctions.Extensions.GooglePubSub {
    public static class PublisherClientCache {
        
        private static ConcurrentDictionary<int, PubSub.PublisherClient> publisherClientV2Cache = new ConcurrentDictionary<int, PubSub.PublisherClient>();

        internal static PubSub.PublisherClient GetPublisherClientV2(GooglePubSubAttribute googlePubSubAttribute) {

            var key = googlePubSubAttribute.GetHashCode();

            if (publisherClientV2Cache.ContainsKey(key)) {
                return publisherClientV2Cache[key];
            } else {
                var credentials = CreatorService.GetCredentials(googlePubSubAttribute);

                PubSub.PublisherClient publisherClient = new PubSub.PublisherClient(credentials, googlePubSubAttribute.ProjectId, googlePubSubAttribute.TopicId);
                publisherClientV2Cache.AddOrUpdate(key, publisherClient, (newkey, oldValue) => publisherClient);
                return publisherClient;
            }

        }

    }
}
