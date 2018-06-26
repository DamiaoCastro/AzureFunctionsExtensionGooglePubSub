﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using System;

namespace AzureFunctions.Extensions.GooglePubSub {

    public class GooglePubSubExtensionConfig : IExtensionConfigProvider {
        
        public void Initialize(ExtensionConfigContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            Microsoft.Extensions.Logging.ILogger logger = context.Config.LoggerFactory.CreateLogger("GooglePubSubExtension");

            context.AddBindingRule<GooglePubSubAttribute>()
                .BindToCollector(c => new AsyncCollector(c, logger));

            context.Config.RegisterBindingExtensions(new TriggerBindingProvider(logger));
            
        }
    }
}