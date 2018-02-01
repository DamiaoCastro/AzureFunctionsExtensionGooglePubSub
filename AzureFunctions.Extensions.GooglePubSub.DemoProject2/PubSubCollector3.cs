using Microsoft.Azure.WebJobs;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {
    public static class PubSubCollector3 {

        [Disable]
        [FunctionName("PubSubCollector3")]
        public static void Run(
           [TimerTrigger("40 */1 * * * *", RunOnStartup = true)]TimerInfo myTimer,
           [GooglePubSub("MyGooglePubSubConfig2")] ICollector<string> messages
           ) {

            PubSubCollector1.Run(myTimer, messages);

        }
    }
}
