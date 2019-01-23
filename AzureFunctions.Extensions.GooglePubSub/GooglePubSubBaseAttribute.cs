using Microsoft.Azure.WebJobs.Description;
using System;

namespace AzureFunctions.Extensions.GooglePubSub {

    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class GooglePubSubBaseAttribute : Attribute {

        public GooglePubSubBaseAttribute(string credentialsSettingKey) {
            CredentialsSettingKey = credentialsSettingKey;
        }

        public string CredentialsSettingKey { get; }

        private byte[] _credentials = null;

        internal byte[] Credentials {
            get {
                if (_credentials == null) {
                    if (string.IsNullOrWhiteSpace(CredentialsSettingKey)) { throw new ArgumentNullException(nameof(CredentialsSettingKey), $"The property '{nameof(CredentialsSettingKey)}' is not set."); }
                    var credentialsSettingJson = Environment.GetEnvironmentVariable(CredentialsSettingKey, EnvironmentVariableTarget.Process);
                    if (string.IsNullOrWhiteSpace(credentialsSettingJson)) { throw new MissingSettingException($"The setting key '{CredentialsSettingKey}' does not contain a value."); }
                    _credentials = System.Text.Encoding.UTF8.GetBytes(credentialsSettingJson);
                }
                return _credentials;
            }
        }

        internal abstract string GetObjectKey();
    }
}