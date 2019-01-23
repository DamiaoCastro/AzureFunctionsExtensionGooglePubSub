using System;
using System.Collections.Concurrent;

namespace AzureFunctions.Extensions.GooglePubSub.Services {
    internal sealed class ServiceCache : IServiceCache {

        private readonly static ConcurrentDictionary<string, ExpiringService> clientCache = new ConcurrentDictionary<string, ExpiringService>();

        T IServiceCache.GetFromCache<T>(GooglePubSubBaseAttribute googlePubSubAttribute) {
            if (googlePubSubAttribute == null) { throw new ArgumentNullException(nameof(googlePubSubAttribute)); }

            var key = GetKey<T>(googlePubSubAttribute);

            if (clientCache.ContainsKey(key)) {
                var expiringService = clientCache[key];
                if ((DateTime.UtcNow - expiringService.CreatedUtc).TotalMinutes > expiringService.MinutesToCache) {
                    return null;
                }

                return (T)expiringService.Service;
            }

            return null;
        }

        void IServiceCache.SaveToCache<T>(GooglePubSubBaseAttribute googlePubSubAttribute, T service, int minutesToCache) {
            if (googlePubSubAttribute == null) { throw new ArgumentNullException(nameof(googlePubSubAttribute)); }
            if (service == null) { throw new ArgumentNullException(nameof(service)); }
            if (minutesToCache < 0) { throw new ArgumentOutOfRangeException(nameof(minutesToCache), $"The parameter '{nameof(minutesToCache)}' must be positive"); }

            var key = GetKey<T>(googlePubSubAttribute);
            var expiringService = new ExpiringService(DateTime.UtcNow, service, minutesToCache);
            clientCache.AddOrUpdate(key, expiringService, (newkey, oldValue) => expiringService);

        }

        private string GetKey<T>(GooglePubSubBaseAttribute googlePubSubAttribute) {
            return $"{typeof(T)}|{googlePubSubAttribute.GetObjectKey()}"; ;
        }

        private class ExpiringService {

            public ExpiringService(DateTime createdUtc, object service, int minutesToCache) {
                CreatedUtc = createdUtc;
                Service = service;
                MinutesToCache = minutesToCache;
            }

            public DateTime CreatedUtc { get; }
            public object Service { get; }
            public double MinutesToCache { get; }

        }

    }
}