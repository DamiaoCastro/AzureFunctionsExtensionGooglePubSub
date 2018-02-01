using Microsoft.Azure.WebJobs;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {
    public static class PubSubCollector2 {

        [Disable]
        [FunctionName("PubSubCollector2")]
        public static void Run(
           [TimerTrigger("20 */1 * * * *", RunOnStartup = true)]TimerInfo myTimer,
           [GooglePubSub("MyGooglePubSubConfig2")] ICollector<string> messages
           ) {

            PubSubCollector1.Run(myTimer, messages);

        }
    }
}
