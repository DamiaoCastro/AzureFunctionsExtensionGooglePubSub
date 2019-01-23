using AzureFunctions.Extensions.GooglePubSub.Services;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TransparentApiClient.Google.PubSub.V1.Resources;
using TransparentApiClient.Google.PubSub.V1.Schema;

namespace AzureFunctions.Extensions.GooglePubSub.Bindings {

    internal sealed class AsyncCollector : ICollector<PubsubMessage>, IAsyncCollector<PubsubMessage> {

        private readonly GooglePubSubCollectorAttribute googlePubSubAttribute;
        private readonly IServiceFactory serviceFactory;

        private readonly List<PubsubMessage> items = new List<PubsubMessage>();

        public AsyncCollector(GooglePubSubCollectorAttribute googlePubSubAttribute, IServiceFactory serviceFactory) {
            this.googlePubSubAttribute = googlePubSubAttribute;
            this.serviceFactory = serviceFactory;
        }

        void ICollector<PubsubMessage>.Add(PubsubMessage item) {
            items.Add(item);
        }

        Task IAsyncCollector<PubsubMessage>.AddAsync(PubsubMessage item, CancellationToken cancellationToken) {
            items.Add(item);
            return Task.CompletedTask;
        }

        Task IAsyncCollector<PubsubMessage>.FlushAsync(CancellationToken cancellationToken) {

            if (items.Any()) {

                var topics = serviceFactory.GetService<ITopics>(googlePubSubAttribute);

                return
                    topics
                        .PublishAsync($"projects/{googlePubSubAttribute.ProjectId}/topics/{googlePubSubAttribute.TopicId}",
                            new PublishRequest() {
                                messages = items
                            }, null, cancellationToken)
                            .ContinueWith((publishTask) => {
                                if (publishTask.IsFaulted) {
                                    throw publishTask.Exception;
                                } else {
                                    var result = publishTask.Result;
                                    if (result.Success) {
                                        //...
                                    } else {
                                        throw new Exception(string.IsNullOrWhiteSpace(result.ErrorText) ? result.Error?.message : result.ErrorText);
                                    }
                                }
                            });

            }

            return Task.CompletedTask;
        }

    }
}