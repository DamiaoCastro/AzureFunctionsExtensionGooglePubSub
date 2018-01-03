﻿using Google.Cloud.PubSub.V1;
using Grpc.Auth;
using Grpc.Core;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureFunctions.Extensions.GooglePubSub {

    internal class AsyncCollector : ICollector<string>, IAsyncCollector<string> {

        private readonly GooglePubSubAttribute googlePubSubAttribute;
        private List<string> items = new List<string>();

        public AsyncCollector(GooglePubSubAttribute googlePubSubAttribute) {
            this.googlePubSubAttribute = googlePubSubAttribute;
        }

        void ICollector<string>.Add(string item) {
            items.Add(item);
        }

        Task IAsyncCollector<string>.AddAsync(string item, CancellationToken cancellationToken) {
            items.Add(item);
            return Task.WhenAll();
        }

        Task IAsyncCollector<string>.FlushAsync(CancellationToken cancellationToken) {

            if (items.Any()) {
                PublisherClient publisher = CreatorService.GetPublisherClient(googlePubSubAttribute.CredentialsFileName);

                var topicName = new TopicName(googlePubSubAttribute.ProjectId, googlePubSubAttribute.TopicId);
                var pubSubMessages = items.Select(c => new PubsubMessage() { Data = Google.Protobuf.ByteString.CopyFromUtf8(c) });
                return publisher.PublishAsync(topicName, pubSubMessages, cancellationToken);
            }

            return Task.WhenAll();
        }

    }
}