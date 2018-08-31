using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Logging;
using System;

namespace AzureFunctions.Extensions.GooglePubSub
{
    [Extension("googlePubSub")]
    public class GooglePubSubExtensionConfig : IExtensionConfigProvider
    {
        private readonly ILoggerFactory _loggerFactory;
        public GooglePubSubExtensionConfig(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            
            ILogger logger = _loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("GooglePubSubExtension"));
            
            context.AddBindingRule<GooglePubSubAttribute>()
                .BindToCollector(c => new AsyncCollector(c, logger));

            context.AddBindingRule<GooglePubSubTriggerAttribute>()
                .BindToTrigger(new TriggerBindingProvider(logger));

        }
        
    }
}