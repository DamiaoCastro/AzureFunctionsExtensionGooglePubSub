using Microsoft.Azure.WebJobs;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {
    public static class PubSubCollector1 {

        [FunctionName("PubSubCollector1")]
        public static void Run(
           [TimerTrigger("0 */1 * * * *", RunOnStartup = true)]TimerInfo myTimer,
           [GooglePubSub("MyGooglePubSubConfig2")] ICollector<string> messages
           ) {

            for (int i = 0; i < 1; i++) {
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
                messages.Add("I have a new message from PubSubCollector1 xxxx");
            }

        }
    }
}
