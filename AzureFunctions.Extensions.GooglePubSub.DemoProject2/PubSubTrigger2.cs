using Microsoft.Azure.WebJobs;
using System.Collections.Generic;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {

    public static class PubSubTrigger2 {

        [Disable]
        [FunctionName("PubSubTrigger2")]
        public static void Run(
            [GooglePubSubTrigger("MyGooglePubSubConfig2")]
                IEnumerable<string> messages) {

            foreach (var message in messages) {
                System.Diagnostics.Debug.WriteLine(message);
            }

        }

    }
}
