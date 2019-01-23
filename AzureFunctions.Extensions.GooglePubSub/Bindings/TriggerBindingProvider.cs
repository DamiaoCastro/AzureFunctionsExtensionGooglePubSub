using AzureFunctions.Extensions.GooglePubSub.Services;
using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TransparentApiClient.Google.PubSub.V1.Schema;

namespace AzureFunctions.Extensions.GooglePubSub.Bindings {

    internal class TriggerBindingProvider : ITriggerBindingProvider {

        private readonly IServiceFactory serviceFactory;

        public TriggerBindingProvider(IServiceFactory serviceFactory) {
            this.serviceFactory = serviceFactory;
        }

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
            if (
                //parameter.ParameterType != typeof(IEnumerable<string>) &&
                //parameter.ParameterType != typeof(string[]) &&
                parameter.ParameterType != typeof(IEnumerable<PubsubMessage>) &&
                parameter.ParameterType != typeof(PubsubMessage[])
                ) {

                throw new InvalidOperationException($"Can't bind {nameof(GooglePubSubTriggerAttribute)} to type '{parameter.ParameterType}'.");

            }
            
            return Task.FromResult<ITriggerBinding>(new TriggerBinding(attribute, context.Parameter, serviceFactory));

        }

    }
}