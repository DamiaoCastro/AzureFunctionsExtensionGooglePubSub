namespace AzureFunctions.Extensions.GooglePubSub {
    internal class CreatorService {
        
        public static byte[] GetCredentials(GooglePubSubTriggerAttribute googlePubSubTriggerAttribute)
        {
            if (googlePubSubTriggerAttribute.Credentials != null)
            {
                return googlePubSubTriggerAttribute.Credentials;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(googlePubSubTriggerAttribute.CredentialsFileName))
                {
                    return GetCredentials(googlePubSubTriggerAttribute.CredentialsFileName);
                }
            }

            return null;
        }

        internal static byte[] GetCredentials(GooglePubSubAttribute googlePubSubAttribute) {
            if (googlePubSubAttribute.Credentials != null) {
                return googlePubSubAttribute.Credentials;
            } else {
                if (!string.IsNullOrWhiteSpace(googlePubSubAttribute.CredentialsFileName)) {
                    return GetCredentials(googlePubSubAttribute.CredentialsFileName);
                }
            }

            return null;
        }
        
        private static byte[] GetCredentials(string credentialsFileName)
        {
            var path = System.IO.Path.GetDirectoryName(typeof(TriggerBindingProvider).Assembly.Location);
            var fullPath = System.IO.Path.Combine(path, "..", credentialsFileName);
            var credentials = System.IO.File.ReadAllBytes(fullPath);
            return credentials;
        }
        
    }
}