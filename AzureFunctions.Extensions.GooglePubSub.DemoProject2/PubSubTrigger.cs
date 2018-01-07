using Microsoft.Azure.WebJobs;
using System.Collections.Generic;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {

    public static class PubSubTrigger {

        [FunctionName("PubSubTrigger")]
        public static void Run(
            [GooglePubSubTrigger("MyGooglePubSubConfig")]
                IEnumerable<string> messages) {

            foreach (var message in messages) {
                System.Console.WriteLine(message);
            }

        }

    }
}
