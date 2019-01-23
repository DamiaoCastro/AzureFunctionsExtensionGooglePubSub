using System;
using TransparentApiClient.Google.PubSub.V1.Resources;

namespace AzureFunctions.Extensions.GooglePubSub.Services {
    internal class ServiceFactory : IServiceFactory {

        private readonly IServiceCache serviceCache;

        public ServiceFactory(IServiceCache serviceCache) {
            this.serviceCache = serviceCache;
        }

        T IServiceFactory.GetService<T>(GooglePubSubBaseAttribute googlePubSubAttribute) {

            T service = serviceCache.GetFromCache<T>(googlePubSubAttribute);
            if (service is null) {
                service = CreateResourceService<T>(googlePubSubAttribute);
                serviceCache.SaveToCache<T>(googlePubSubAttribute, service, 10);
            }

            return service;
        }
        
        private T CreateResourceService<T>(GooglePubSubBaseAttribute googlePubSubAttribute) where T : class {

            byte[] credentials = googlePubSubAttribute.Credentials;
            
            if (typeof(T) == typeof(ITopics)) { return (T)(ITopics)new Topics(credentials); }
            if (typeof(T) == typeof(ISnapshots)) { return (T)(ISnapshots)new Snapshots(credentials); }
            if (typeof(T) == typeof(ISubscriptions)) { return (T)(ISubscriptions)new Subscriptions(credentials); }

            throw new NotImplementedException();
        }

    }
}