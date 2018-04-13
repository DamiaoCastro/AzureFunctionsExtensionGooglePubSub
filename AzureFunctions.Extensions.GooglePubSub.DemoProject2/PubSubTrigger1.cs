using Microsoft.Azure.WebJobs;
using System.Collections.Generic;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {

    public static class PubSubTrigger1 {

        [Disable]
        [FunctionName("PubSubTrigger1")]
        public static void Run(
            [GooglePubSubTrigger("MyGooglePubSubConfig1")]
                IEnumerable<string> messages,
           [GooglePubSub("MyGooglePubSubConfig2")] ICollector<string> messagesCollector
           ) {

            //foreach (var message in messages) {
            //    System.Console.WriteLine(message);
            //}

            PubSubCollector1.Run(null, messagesCollector);


        }

    }
}
