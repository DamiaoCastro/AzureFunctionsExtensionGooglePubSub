using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace AzureFunctions.Extensions.GooglePubSub {

    internal class TriggerBindingProvider : ITriggerBindingProvider {
        
        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            
            ParameterInfo parameter = context.Parameter;
            GooglePubSubTriggerAttribute attribute = parameter.GetCustomAttribute<GooglePubSubTriggerAttribute>(inherit: false);
            if (attribute == null) {
                return Task.FromResult<ITriggerBinding>(null);
            }
            
            // TODO: Define the types your binding supports here
            if (parameter.ParameterType != typeof(IEnumerable<string>) &&
                parameter.ParameterType != typeof(string[])) {

                throw new InvalidOperationException($"Can't bind {nameof(GooglePubSubTriggerAttribute)} to type '{parameter.ParameterType}'.");

            }
            
            return Task.FromResult<ITriggerBinding>(new TriggerBinding(attribute, context.Parameter));

        }

    }
}