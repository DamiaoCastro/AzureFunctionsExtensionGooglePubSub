namespace AzureFunctions.Extensions.GooglePubSub.Services {
    internal interface IServiceCache {

        T GetFromCache<T>(GooglePubSubBaseAttribute googlePubSubAttribute) where T : class;

        void SaveToCache<T>(GooglePubSubBaseAttribute googlePubSubAttribute, T service, int minutesToCache) where T : class;

    }
}
