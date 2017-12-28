﻿using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Config;
using System;

namespace AzureFunctions.Extensions.GooglePubSub {

    public class GooglePubSubExtensionConfig : IExtensionConfigProvider {
        
        public void Initialize(ExtensionConfigContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            context.Config.RegisterBindingExtensions(new TriggerBindingProvider());
            
            //context.AddBindingRule<GooglePubSubTriggerAttribute>()
            //    .BindToTrigger(new TriggerAttributeBindingProvider());
            
        }
    }
}