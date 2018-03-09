using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AzureFunctions.Extensions.GooglePubSub.PubSub {

    //https://cloud.google.com/pubsub/docs/reference/rest/
    //https://developers.google.com/identity/protocols/OAuth2ServiceAccount#creatingjwt

    internal class SubscriberClient : PubSubBaseClient {

        private readonly string subscriptionId;

        public SubscriberClient(byte[] serviceAccountCredentials, string projectId, string subscriptionId) : base(serviceAccountCredentials, projectId) {

            if (string.IsNullOrWhiteSpace(subscriptionId)) { throw new System.ArgumentNullException(nameof(subscriptionId)); }

            this.subscriptionId = subscriptionId;
        }

        public Task<CreateSubscriptionResult> CreateAsync(CreateSubscriptionRequest createSubscriptionRequest, CancellationToken cancellationToken) {

            return SendAsync(HttpMethod.Put, $"subscriptions/{createSubscriptionRequest.name}", createSubscriptionRequest, cancellationToken)
                .ContinueWith((sendTask) => {

                    HttpResponseMessage httpResponse = sendTask.Result;

                    if (httpResponse.IsSuccessStatusCode) {
                        //return httpResponse.Content.ReadAsStringAsync()
                        //    .ContinueWith((readAsStringTask) => {
                        //        string resultString = readAsStringTask.Result;
                        //        return JsonConvert.DeserializeObject<SubscriberPullResponse>(resultString);
                        //    });
                        return CreateSubscriptionResult.Success;
                    } else {
                        if (httpResponse.StatusCode == System.Net.HttpStatusCode.Conflict) {
                            //already existed
                            return CreateSubscriptionResult.AlreadyExisted;
                        } else {
                            return CreateSubscriptionResult.Error;
                        }
                    }

                });

        }

        public Task<SubscriberPullResponse> PullAsync(int maxMessages, bool returnImmediately, CancellationToken cancellationToken) {

            return SendAsync(HttpMethod.Post, $"subscriptions/{subscriptionId}:pull", new { maxMessages, returnImmediately }, cancellationToken)
                .ContinueWith((postTask) => {

                    HttpResponseMessage post = postTask.Result;

                    if (post.IsSuccessStatusCode) {

                        if (post.Content.Headers.ContentEncoding.Contains("gzip")) {

                            return post.Content.ReadAsByteArrayAsync()
                                .ContinueWith((readAsByteArrayTask) => {
                                    byte[] resultByteArray = readAsByteArrayTask.Result;

                                    var memoryStream = new MemoryStream();
                                    using (var zipStream = new System.IO.Compression.GZipStream(new MemoryStream(resultByteArray), System.IO.Compression.CompressionMode.Decompress)) {
                                        zipStream.CopyTo(memoryStream);
                                    }

                                    var resultString = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                                    var publishResponse = JsonConvert.DeserializeObject<SubscriberPullResponse>(resultString);

                                    return publishResponse;
                                });

                        } else {
                            return post.Content.ReadAsStringAsync()
                                .ContinueWith((readAsStringTask) => {
                                    string resultString = readAsStringTask.Result;
                                    return JsonConvert.DeserializeObject<SubscriberPullResponse>(resultString);
                                });
                        }
                    }

                    return Task.FromResult<SubscriberPullResponse>(null);
                }).Unwrap();

        }

        public Task AcknowledgeAsync(IEnumerable<string> ackIds, CancellationToken cancellationToken) {

            return SendAsync(HttpMethod.Post, $"subscriptions/{subscriptionId}:acknowledge", new { ackIds }, cancellationToken)
                .ContinueWith((postTask) => {
                    HttpResponseMessage post = postTask.Result;
                    if (!post.IsSuccessStatusCode) {
                        throw new FailedToAcknowledgeException();
                    }
                });

        }

    }
}