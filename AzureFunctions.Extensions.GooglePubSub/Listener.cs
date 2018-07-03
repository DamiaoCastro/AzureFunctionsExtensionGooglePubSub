using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using System;
using System.Linq;
using System.Collections.Generic;
using TransparentApiClient.Google.Core;
using TransparentApiClient.Google.PubSub.V1.Schema;
using TransparentApiClient.Google.PubSub.V1.Resources;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Extensions.GooglePubSub
{
    internal class Listener : IListener
    {

        private ITriggeredFunctionExecutor executor;
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly GooglePubSubTriggerAttribute triggerAttribute;

        private const int NumberOfMessageBlocks = 10;

        public Listener(ITriggeredFunctionExecutor executor, GooglePubSubTriggerAttribute triggerAttribute, Microsoft.Extensions.Logging.ILogger logger)
        {
            this.executor = executor;
            this.logger = logger;
            this.triggerAttribute = GooglePubSubTriggerAttribute.GetAttributeByConfiguration(triggerAttribute);
        }

        void IListener.Cancel()
        {
        }

        public void Dispose()
        {
        }

        async Task IListener.StartAsync(CancellationToken cancellationToken)
        {

            retry:

            using (var scope = logger.BeginScope("IListener"))
            {

                var eventId = new Microsoft.Extensions.Logging.EventId(1);
                logger.LogInformation("StartAsync");

                try
                {

                    var credentials = CreatorService.GetCredentials(triggerAttribute);
                    var topicsClient = new Topics(credentials);
                    var subscriptionsClient = new Subscriptions(credentials);

                    if (triggerAttribute.CreateSubscriptionIfDoesntExist)
                    {
                        await CreateSubscription(topicsClient, triggerAttribute, cancellationToken);
                        logger.LogInformation("CreateSubscriptionIfDoesntExist");
                    }

                    var index = 0;
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await ListenerPull(topicsClient, subscriptionsClient, ++index, cancellationToken);
                        logger.LogInformation("ListenerPull");
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, ex.Message);
                }

            }

            System.Threading.Thread.Sleep(30 * 1000);
            goto retry;

        }

        Task IListener.StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        private Task CreateSubscription(Topics topicsClient, GooglePubSubTriggerAttribute triggerAttribute, CancellationToken cancellationToken)
        {

            var topicName = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}";

            var topic = new TransparentApiClient.Google.PubSub.V1.Schema.Topic()
            {
                name = topicName
                //ackDeadlineSeconds= triggerAttribute.AcknowledgeDeadline,
                //name = triggerAttribute.SubscriptionId,
                //topic = $"projects/{triggerAttribute.ProjectId}/topics/{triggerAttribute.TopicId}"
            };

            return topicsClient.CreateAsync(topicName, topic, null, cancellationToken);

        }

        private Task ListenerPull(Topics topicsClient, Subscriptions subscriptionsClient, int index, CancellationToken cancellationToken)
        {

            var logScope = logger.BeginScope(Guid.NewGuid());

            return GetTypeInput(subscriptionsClient, cancellationToken)
                .ContinueWith((typeInputTask) =>
                {

                    if (typeInputTask.IsFaulted)
                    {
                        logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, new Microsoft.Extensions.Logging.EventId(index, "ListenerPull"), logScope, typeInputTask.Exception, null);
                        throw typeInputTask.Exception;
                    }

                    IEnumerable<(TriggeredFunctionData messages, IEnumerable<string> ackIds)> buckets = typeInputTask.Result;
                    foreach ((TriggeredFunctionData input, IEnumerable<string> ackIds) in buckets)
                    {
                        if (input != null && ackIds != null && ackIds.Any())
                        {

                            var t = executor.TryExecuteAsync(input, cancellationToken)
                                    .ContinueWith((functionResultTask) =>
                                    {

                                        if (functionResultTask.IsFaulted)
                                        {
                                            logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, new Microsoft.Extensions.Logging.EventId(index, "TryExecuteAsync"), logScope, functionResultTask.Exception, null);
                                        }
                                        else
                                        {
                                            FunctionResult functionResult = functionResultTask.Result;
                                            if (functionResult.Succeeded)
                                            {
                                                return AcknowledgeAsync(subscriptionsClient, ackIds, cancellationToken);
                                            }
                                            else
                                            {
                                                logger.Log(Microsoft.Extensions.Logging.LogLevel.Error, new Microsoft.Extensions.Logging.EventId(index, "TryExecuteAsync"), logScope, functionResult.Exception, null);
                                            }
                                        }

                                        return Task.CompletedTask;

                                    }, cancellationToken).Unwrap();

                            return t;
                        }
                    }

                    return Task.CompletedTask;

                }).Unwrap();

        }

        private Task AcknowledgeAsync(Subscriptions subscriptionsClient, IEnumerable<string> ackIds, CancellationToken cancellationToken)
        {

            return subscriptionsClient.AcknowledgeAsync($"projects/{triggerAttribute.ProjectId}/subscriptions/{triggerAttribute.SubscriptionId}",
                new AcknowledgeRequest()
                {
                    ackIds = ackIds
                },
                null, cancellationToken);

        }

        private Task<IEnumerable<(TriggeredFunctionData messages, IEnumerable<string> ackIds)>> GetTypeInput(Subscriptions subscriptionsClient, CancellationToken cancellationToken)
        {

            var maxMessages = triggerAttribute.MaxBatchSize * NumberOfMessageBlocks;

            return subscriptionsClient.PullAsync($"projects/{triggerAttribute.ProjectId}/subscriptions/{triggerAttribute.SubscriptionId}",
                    new PullRequest()
                    {
                        maxMessages = maxMessages,
                        returnImmediately = false
                    },
                    null,
                    cancellationToken)
                .ContinueWith(getMessages);

        }

        private IEnumerable<(TriggeredFunctionData messages, IEnumerable<string> ackIds)> getMessages(Task<BaseResponse<PullResponse>> pullTask)
        {

            var pull = pullTask.Result;

            if (pull != null && pull.Success && pull.Response.receivedMessages != null && pull.Response.receivedMessages.Count() > 0)
            {

                for (int i = 0; i < NumberOfMessageBlocks; i++)
                {

                    var messagesBlock = pull.Response.receivedMessages.Skip(i * triggerAttribute.MaxBatchSize).Take(triggerAttribute.MaxBatchSize);

                    IEnumerable<string> messages = messagesBlock.Select(c => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(c.message.data)));
                    IEnumerable<string> ackIds = messagesBlock.Select(c => c.ackId);

                    TriggeredFunctionData input = new TriggeredFunctionData
                    {
                        TriggerValue = messages
                    };

                    yield return (input, ackIds);
                }
            }
            else
            {
                if (pull != null && (!pull.Success))
                {
                    throw new Exception(pull.ErrorText);
                }
            }

        }

    }
}