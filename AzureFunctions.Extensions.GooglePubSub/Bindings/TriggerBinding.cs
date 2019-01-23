using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System.Threading.Tasks;
using System.Reflection;
using AzureFunctions.Extensions.GooglePubSub.Services;
using TransparentApiClient.Google.PubSub.V1.Schema;

namespace AzureFunctions.Extensions.GooglePubSub.Bindings {

    internal class TriggerBinding : ITriggerBinding {

        private readonly GooglePubSubTriggerAttribute googlePubSubTriggerAttribute;
        private readonly ParameterInfo parameter;
        private readonly IServiceFactory serviceFactory;

        private readonly IReadOnlyDictionary<string, Type> bindingContract;

        public TriggerBinding(GooglePubSubTriggerAttribute googlePubSubTriggerAttribute, ParameterInfo parameter, IServiceFactory serviceFactory) {
            this.googlePubSubTriggerAttribute = googlePubSubTriggerAttribute;
            this.parameter = parameter;
            this.serviceFactory = serviceFactory;
            bindingContract = CreateBindingDataContract();
        }

        Type ITriggerBinding.TriggerValueType => typeof(IEnumerable<PubsubMessage>);

        IReadOnlyDictionary<string, Type> ITriggerBinding.BindingDataContract => bindingContract;

        Task<ITriggerData> ITriggerBinding.BindAsync(object value, ValueBindingContext context) {
            // TODO: Perform any required conversions on the value
            // E.g. convert from Dashboard invoke string to our trigger
            // value type
            IEnumerable<PubsubMessage> triggerValue = value as IEnumerable<PubsubMessage>;
            IValueBinder valueBinder = new ValueBinder(parameter, triggerValue);
            return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
        }

        Task<IListener> ITriggerBinding.CreateListenerAsync(ListenerFactoryContext context) {
            return Task.FromResult<IListener>(new Listener(context.Executor, googlePubSubTriggerAttribute, serviceFactory));
        }

        ParameterDescriptor ITriggerBinding.ToParameterDescriptor() {
            return new GooglePubSubTriggerParameterDescriptor {
                Name = parameter.Name,
                DisplayHints = new ParameterDisplayHints {
                    // TODO: Customize your Dashboard display strings
                    Prompt = "Sample",
                    Description = "Sample trigger fired",
                    DefaultValue = "Sample"
                }
            };
        }
        
        private class GooglePubSubTriggerParameterDescriptor : TriggerParameterDescriptor {
            public override string GetTriggerReason(IDictionary<string, string> arguments) {
                // TODO: Customize your Dashboard display string
                return string.Format("Sample trigger fired at {0}", DateTime.Now.ToString("o"));
            }
        }

        private IReadOnlyDictionary<string, Type> CreateBindingDataContract() {
            Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            contract.Add(nameof(GooglePubSubTriggerAttribute), typeof(GooglePubSubTriggerAttribute));
            contract.Add(nameof(PubsubMessage), typeof(PubsubMessage));
            return contract;
        }

        private IReadOnlyDictionary<string, object> GetBindingData(IEnumerable<PubsubMessage> value) {
            Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) {
                { "GooglePubSubTrigger", value }
            };
            return bindingData;
        }

    }
}