using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using System.Collections.Generic;

namespace AzureFunctions.Extensions.GooglePubSub {
    internal class ValueBinder : IValueBinder {

        private object _parameter;
        private object value;

        public ValueBinder(object parameter, IEnumerable<string> triggerValue) {
            _parameter = parameter;
            this.value = triggerValue;
        }

        public Type Type { get { return typeof(GooglePubSubTriggerAttribute); } }

        public Task<object> GetValueAsync() {
            // TODO: Perform any required conversions
            if (Type == typeof(string)) {
                return Task.FromResult<object>(value.ToString());
            }
            return Task.FromResult(value);
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken) {
            this.value = value;

            return Task.FromResult(true);
        }

        public string ToInvokeString() {
            return "Sample";
        }

    }
}