using Microsoft.Azure.WebJobs;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject1 {
    public static class PubSubCollector {

        [FunctionName("PubSubCollector")]
        public static void Run(
            [TimerTrigger("0 */1 * * * *", RunOnStartup = true)]TimerInfo myTimer,
            [GooglePubSub("credencials.json", "projectId", "topicId")] ICollector<string> messages
            ) {

            messages.Add("I have a new message");

        }

    }
}
