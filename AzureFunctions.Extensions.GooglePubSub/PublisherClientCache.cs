using System.Collections.Concurrent;

namespace AzureFunctions.Extensions.GooglePubSub {
    public static class PublisherClientCache {
        
        private static ConcurrentDictionary<int, TransparentApiClient.Google.PubSub.V1.Resources.Topics> publisherClientV2Cache = new ConcurrentDictionary<int, TransparentApiClient.Google.PubSub.V1.Resources.Topics>();

        internal static TransparentApiClient.Google.PubSub.V1.Resources.Topics GetTopicsClient(GooglePubSubAttribute googlePubSubAttribute) {

            var key = googlePubSubAttribute.GetHashCode();

            if (publisherClientV2Cache.ContainsKey(key)) {
                return publisherClientV2Cache[key];
            } else {
                var credentials = CreatorService.GetCredentials(googlePubSubAttribute);
                var publisherClient = new TransparentApiClient.Google.PubSub.V1.Resources.Topics(credentials);
                publisherClientV2Cache.AddOrUpdate(key, publisherClient, (newkey, oldValue) => publisherClient);
                return publisherClient;
            }

        }

    }
}
