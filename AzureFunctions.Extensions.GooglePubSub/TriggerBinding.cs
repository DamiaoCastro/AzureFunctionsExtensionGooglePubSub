using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System.Threading.Tasks;
using System.Reflection;

namespace AzureFunctions.Extensions.GooglePubSub {

    internal class TriggerBinding : ITriggerBinding {

        private readonly GooglePubSubTriggerAttribute googlePubSubTriggerAttribute;
        private ParameterInfo parameter;
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private IReadOnlyDictionary<string, Type> bindingContract;

        public TriggerBinding(GooglePubSubTriggerAttribute googlePubSubTriggerAttribute, ParameterInfo parameter, Microsoft.Extensions.Logging.ILogger logger) {
            this.googlePubSubTriggerAttribute = googlePubSubTriggerAttribute;
            this.parameter = parameter;
            this.logger = logger;
            bindingContract = CreateBindingDataContract();
        }

        IReadOnlyDictionary<string, Type> ITriggerBinding.BindingDataContract => bindingContract;

        Type ITriggerBinding.TriggerValueType { get { return typeof(IEnumerable<string>); } }

        Task<ITriggerData> ITriggerBinding.BindAsync(object value, ValueBindingContext context) {
            // TODO: Perform any required conversions on the value
            // E.g. convert from Dashboard invoke string to our trigger
            // value type
            IEnumerable<string> triggerValue = value as IEnumerable<string>;
            IValueBinder valueBinder = new ValueBinder(parameter, triggerValue);
            return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
        }

        Task<IListener> ITriggerBinding.CreateListenerAsync(ListenerFactoryContext context) {
            return Task.FromResult<IListener>(new Listener(context.Executor, googlePubSubTriggerAttribute, logger));
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
            //contract.Add("GooglePubSubTriggerAttribute", typeof(GooglePubSubTriggerAttribute));

            // TODO: Add any additional binding contract members

            return contract;
        }

        private IReadOnlyDictionary<string, object> GetBindingData(IEnumerable<string> value) {
            Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            bindingData.Add("GooglePubSubTrigger", value);
            return bindingData;
        }

    }
}