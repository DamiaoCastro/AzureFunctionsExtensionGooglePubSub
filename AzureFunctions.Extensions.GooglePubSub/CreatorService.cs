using Google.Cloud.PubSub.V1;
using Grpc.Auth;
using Grpc.Core;

namespace AzureFunctions.Extensions.GooglePubSub {
    internal class CreatorService {

        public static Publisher.PublisherClient GetPublisherClient(GooglePubSubAttribute googlePubSubAttribute) {

            Channel channel = null;
            if (googlePubSubAttribute.Credentials != null) {
                channel = GetChannel(googlePubSubAttribute.Credentials);
            } else {
                if (!string.IsNullOrWhiteSpace(googlePubSubAttribute.CredentialsFileName)) {
                    channel = GetChannel(googlePubSubAttribute.CredentialsFileName);
                } else {
                    channel = new Grpc.Core.Channel("pubsub.googleapis.com", ChannelCredentials.Insecure);
                }
            }

            return new Publisher.PublisherClient(channel);
        }

        public static Subscriber.SubscriberClient GetSubscriberClient(GooglePubSubTriggerAttribute googlePubSubTriggerAttribute) {
            Channel channel = null;
            if (googlePubSubTriggerAttribute.Credentials != null) {
                channel = GetChannel(googlePubSubTriggerAttribute.Credentials);
            } else {
                if (!string.IsNullOrWhiteSpace(googlePubSubTriggerAttribute.CredentialsFileName)) {
                    channel = GetChannel(googlePubSubTriggerAttribute.CredentialsFileName);
                }
            }

            return new Subscriber.SubscriberClient(channel);
        }

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

        public static Grpc.Core.Channel GetChannel(string credentialsFileName)
        {
            if (string.IsNullOrWhiteSpace(credentialsFileName)) { return null; }

            byte[] credentials = GetCredentials(credentialsFileName);
            return GetChannel(credentials);
        }

        private static byte[] GetCredentials(string credentialsFileName)
        {
            var path = System.IO.Path.GetDirectoryName(typeof(TriggerBindingProvider).Assembly.Location);
            var fullPath = System.IO.Path.Combine(path, "..", credentialsFileName);
            var credentials = System.IO.File.ReadAllBytes(fullPath);
            return credentials;
        }

        private static Grpc.Core.Channel GetChannel(byte[] credentials) {
            var googleCredential = Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(new System.IO.MemoryStream(credentials)).CreateScoped("https://www.googleapis.com/auth/pubsub");
            
            ChannelCredentials channelCredentials = googleCredential.ToChannelCredentials();
            
            Grpc.Core.Channel channel = new Grpc.Core.Channel("pubsub.googleapis.com", channelCredentials);
            return channel;
        }

    }
}