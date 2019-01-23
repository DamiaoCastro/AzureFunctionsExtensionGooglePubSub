using AzureFunctions.Extensions.GooglePubSub.Bindings;
using AzureFunctions.Extensions.GooglePubSub.Services;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using System;
using System.Collections.Generic;
using TransparentApiClient.Google.PubSub.V1.Schema;

namespace AzureFunctions.Extensions.GooglePubSub.Config {
    [Extension("GooglePubSub")]
    public class GooglePubSubExtensionConfig : IExtensionConfigProvider {
        
        public void Initialize(ExtensionConfigContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            IServiceCache serviceCache = new ServiceCache();
            IServiceFactory serviceFactory = new ServiceFactory(serviceCache);

            context.AddConverter<string, PubsubMessage>(c => GetPubsubMessage(c));

            context
                .AddBindingRule<GooglePubSubCollectorAttribute>()
                .BindToCollector(c => new AsyncCollector(c, serviceFactory));

            context.AddBindingRule<GooglePubSubTriggerAttribute>()
                .BindToTrigger<IEnumerable<PubsubMessage>>(new TriggerBindingProvider(serviceFactory));

        }

        private PubsubMessage GetPubsubMessage(string input) {
            return new PubsubMessage() {
                data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input))
            };
        }

    }
}