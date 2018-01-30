using Microsoft.Azure.WebJobs;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {
    public static class PubSubCollector3 {

        [Disable]
        [FunctionName("PubSubCollector3")]
        public static void Run(
           [TimerTrigger("0 */1 * * * *", RunOnStartup = true)]TimerInfo myTimer,
           [GooglePubSub("MyGooglePubSubConfig3")] ICollector<string> messages
           ) {

            messages.Add("I have a new message from PubSubCollector3");

        }
    }
}
