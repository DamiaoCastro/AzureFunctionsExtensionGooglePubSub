using Microsoft.Azure.WebJobs;
using System.Collections.Generic;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {
    public static class PubSubTrigger {
        [FunctionName("PubSubTrigger")]
        public static void Run(
            [GooglePubSubTrigger("", "projectId", "topicId", "subscriptionId", CreateSubscriptionIfDoesntExist = true, MaxBatchSize = 1000)]
                IEnumerable<string> messages) {

            foreach (var message in messages) {
                System.Console.WriteLine(message);
            }

        }

    }
}
