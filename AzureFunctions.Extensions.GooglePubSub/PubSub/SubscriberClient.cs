using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AzureFunctions.Extensions.GooglePubSub.PubSub
{

    //https://cloud.google.com/pubsub/docs/reference/rest/
    //https://developers.google.com/identity/protocols/OAuth2ServiceAccount#creatingjwt

    internal class SubscriberClient : IDisposable
    {
        private readonly byte[] serviceAccountCredentials;
        private readonly string projectId;
        private readonly string subscriptionId;
        private System.Net.Http.HttpClient httpClient = null;

        public SubscriberClient(byte[] serviceAccountCredentials, string projectId, string subscriptionId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("message", nameof(projectId));
            }

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentException("message", nameof(subscriptionId));
            }

            this.serviceAccountCredentials = serviceAccountCredentials ?? throw new ArgumentNullException(nameof(serviceAccountCredentials));
            this.projectId = projectId;
            this.subscriptionId = subscriptionId;
        }

        public Task<SubscriberPullResponse> PullAsync(int maxMessages, bool returnImmediately, CancellationToken cancellationToken)
        {
            var httpClient = GetHttpClient();

            var content = new System.Net.Http.StringContent(JsonConvert.SerializeObject(new { maxMessages, returnImmediately }), System.Text.Encoding.UTF8, "application/json");
            return httpClient.PostAsync($"https://pubsub.googleapis.com/v1/projects/{projectId}/subscriptions/{subscriptionId}:pull?alt=json", content, cancellationToken)
                .ContinueWith((postTask) =>
                {

                    HttpResponseMessage post = postTask.Result;

                    if (post.IsSuccessStatusCode)
                    {
                        return post.Content.ReadAsStringAsync()
                            .ContinueWith((readAsStringTask) =>
                            {
                                string resultString = readAsStringTask.Result;
                                return JsonConvert.DeserializeObject<SubscriberPullResponse>(resultString);
                            });
                    }

                    return Task.FromResult<SubscriberPullResponse>(null);
                }).Unwrap();

        }

        internal Task AcknowledgeAsync(IEnumerable<string> ackIds, CancellationToken cancellationToken)
        {
            var httpClient = GetHttpClient();

            var content = new System.Net.Http.StringContent(JsonConvert.SerializeObject(new { ackIds }), System.Text.Encoding.UTF8, "application/json");
            return httpClient.PostAsync($"https://pubsub.googleapis.com/v1/projects/{projectId}/subscriptions/{subscriptionId}:acknowledge", content, cancellationToken)
                .ContinueWith((postTask) =>
                {
                    HttpResponseMessage post = postTask.Result;
                    if (!post.IsSuccessStatusCode)
                    {
                        throw new FailedToAcknowledgeException();
                    }
                });

        }

        DateTime t1 = DateTime.UtcNow;
        private System.Net.Http.HttpClient GetHttpClient()
        {
            if ((DateTime.UtcNow - t1).TotalHours > 1)//get new http client and new credentials every hour
            {
                httpClient.Dispose();
                httpClient = null;
                t1 = DateTime.UtcNow;
            }

            if (httpClient == null)
            {
                var googleCredential = Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(new System.IO.MemoryStream(serviceAccountCredentials))
                                            .CreateScoped("https://www.googleapis.com/auth/pubsub");
                var accessToken = googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync().Result;


                httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            return httpClient;
        }

        void IDisposable.Dispose()
        {
            httpClient?.Dispose();
        }

    }
}