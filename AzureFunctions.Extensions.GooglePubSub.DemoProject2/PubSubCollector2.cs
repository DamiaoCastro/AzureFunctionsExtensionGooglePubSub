using Microsoft.Azure.WebJobs;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {
    public static class PubSubCollector2 {

        [Disable]
        [FunctionName("PubSubCollector2")]
        public static void Run(
           [TimerTrigger("0 */1 * * * *", RunOnStartup = true)]TimerInfo myTimer,
           [GooglePubSub("MyGooglePubSubConfig2")] ICollector<string> messages
           ) {

            messages.Add("I have a new message from PubSubCollector2");

        }
    }
}