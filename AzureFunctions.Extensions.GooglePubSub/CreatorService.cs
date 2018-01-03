using Google.Cloud.PubSub.V1;
using Grpc.Auth;
using Grpc.Core;

namespace AzureFunctions.Extensions.GooglePubSub {
    internal class CreatorService {

        public static PublisherClient GetPublisherClient(string credentialsFileName) {

            var channel = GetChannel(credentialsFileName);
            if (channel == null) {
                return PublisherClient.Create();
            } else {
                return PublisherClient.Create(channel);
            }

        }

        public static SubscriberClient GetSubscriberClient(string credentialsFileName) {

            var channel = GetChannel(credentialsFileName);
            if (channel == null) {
                return SubscriberClient.Create();
            } else {
                return SubscriberClient.Create(channel);
            }

        }

        public static Grpc.Core.Channel GetChannel(string credentialsFileName) {

            if (string.IsNullOrWhiteSpace(credentialsFileName)) { return null; }

            var path = System.IO.Path.GetDirectoryName(typeof(TriggerBindingProvider).Assembly.Location);
            var fullPath = System.IO.Path.Combine(path, "..", credentialsFileName);
            var credentials = System.IO.File.ReadAllBytes(fullPath);
            var googleCredential = Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(new System.IO.MemoryStream(credentials)).CreateScoped(SubscriberClient.DefaultScopes);
            ChannelCredentials channelCredentials = googleCredential.ToChannelCredentials();

            Grpc.Core.Channel channel = new Grpc.Core.Channel(SubscriberClient.DefaultEndpoint.Host, SubscriberClient.DefaultEndpoint.Port, channelCredentials);
            return channel;
        }

    }
}
