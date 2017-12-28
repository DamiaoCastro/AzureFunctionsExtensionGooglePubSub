using Microsoft.Azure.WebJobs.Host.Triggers;
using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

namespace AzureFunctions.Extensions.GooglePubSub {

    internal class TriggerBinding : ITriggerBinding {

        public IReadOnlyDictionary<string, Type> BindingDataContract => bindingContract;

        private ParameterInfo parameter;
        private IReadOnlyDictionary<string, Type> bindingContract;

        public TriggerBinding(ParameterInfo parameter) {
            this.parameter = parameter;
            bindingContract = CreateBindingDataContract();
        }

        public Type TriggerValueType { get { return typeof(IEnumerable<string>); } }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context) {
            // TODO: Perform any required conversions on the value
            // E.g. convert from Dashboard invoke string to our trigger
            // value type
            IEnumerable<string> triggerValue = value as IEnumerable<string>;
            IValueBinder valueBinder = new ValueBinder(parameter, triggerValue);
            return Task.FromResult<ITriggerData>(new TriggerData(valueBinder, GetBindingData(triggerValue)));
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context) {

            GooglePubSubTriggerAttribute googlePubSubTriggerAttribute = null;
            var triggerAttribute = parameter.CustomAttributes.FirstOrDefault(c => c.AttributeType.Name == nameof(GooglePubSubTriggerAttribute));
            if (triggerAttribute != null) {

                var createSubscriptionIfDoesntExist = (bool)triggerAttribute.NamedArguments.FirstOrDefault(c => 
                        c.MemberName == nameof(GooglePubSubTriggerAttribute.CreateSubscriptionIfDoesntExist)
                    ).TypedValue.Value;

                var maxBatchSize = (int)triggerAttribute.NamedArguments.FirstOrDefault(c =>
                        c.MemberName == nameof(GooglePubSubTriggerAttribute.MaxBatchSize)
                    ).TypedValue.Value;

                googlePubSubTriggerAttribute = new GooglePubSubTriggerAttribute(
                    triggerAttribute.ConstructorArguments[0].Value.ToString(),
                    triggerAttribute.ConstructorArguments[1].Value.ToString(),
                    triggerAttribute.ConstructorArguments[2].Value.ToString(),
                    triggerAttribute.ConstructorArguments[3].Value.ToString()
                    ) { CreateSubscriptionIfDoesntExist = createSubscriptionIfDoesntExist, MaxBatchSize = maxBatchSize };
            }

            return Task.FromResult<IListener>(new Listener(context.Executor, googlePubSubTriggerAttribute));
        }

        public ParameterDescriptor ToParameterDescriptor() {
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

            // TODO: Add any additional binding data

            return bindingData;
        }

    }
}