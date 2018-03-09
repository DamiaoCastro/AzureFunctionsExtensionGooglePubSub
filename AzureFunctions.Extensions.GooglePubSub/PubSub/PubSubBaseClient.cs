using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AzureFunctions.Extensions.GooglePubSub.PubSub {
    internal abstract class PubSubBaseClient : IDisposable {

        private readonly byte[] serviceAccountCredentials;
        private readonly string projectId;
        private HttpClient httpClient = null;

        public PubSubBaseClient(byte[] serviceAccountCredentials, string projectId) {
            if (string.IsNullOrWhiteSpace(projectId)) { throw new ArgumentNullException(nameof(projectId)); }

            this.serviceAccountCredentials = serviceAccountCredentials ?? throw new ArgumentNullException(nameof(serviceAccountCredentials));
            this.projectId = projectId;
        }

        protected Task<HttpResponseMessage> SendAsync(HttpMethod httpMethod, string partialUri, object requestBodyObject = null, CancellationToken cancellationToken = default(CancellationToken)) {

            var _httpClient = GetHttpClient();

            HttpContent content = null;
            if (requestBodyObject != null) {
                //content = new StringContent(JsonConvert.SerializeObject(requestBodyObject), System.Text.Encoding.UTF8, "application/json");
                
                byte[] compressedBodyBytes;
                using (var memoryStream = new System.IO.MemoryStream()) {
                    var requestBodyObjectBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestBodyObject));
                    using (var zipStream = new System.IO.Compression.GZipStream(memoryStream, System.IO.Compression.CompressionMode.Compress)) {
                        zipStream.Write(requestBodyObjectBytes, 0, requestBodyObjectBytes.Length);
                    }
                    compressedBodyBytes = memoryStream.ToArray();
                }

                content = new ByteArrayContent(compressedBodyBytes);
            }

            var request = new HttpRequestMessage() {
                RequestUri = new Uri($"https://pubsub.googleapis.com/v1/projects/{projectId}/{partialUri}"),
                Method = httpMethod,
                Content = content
            };
            request.Content.Headers.ContentEncoding.Add("gzip");
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            
            return _httpClient.SendAsync(request, cancellationToken);
        }

        DateTime t1 = DateTime.UtcNow;
        private HttpClient GetHttpClient() {
            if ((DateTime.UtcNow - t1).TotalHours > 1)//get new http client and new credentials every hour
            {
                httpClient.Dispose();
                httpClient = null;
                t1 = DateTime.UtcNow;
            }

            if (httpClient == null) {
                var googleCredential = Google.Apis.Auth.OAuth2.GoogleCredential.FromStream(new System.IO.MemoryStream(serviceAccountCredentials))
                                            .CreateScoped("https://www.googleapis.com/auth/pubsub");
                var accessToken = googleCredential.UnderlyingCredential.GetAccessTokenForRequestAsync().Result;


                httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            return httpClient;
        }

        void IDisposable.Dispose() {
            httpClient?.Dispose();
        }

    }
}
