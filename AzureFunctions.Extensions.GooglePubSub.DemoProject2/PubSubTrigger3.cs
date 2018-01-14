using Microsoft.Azure.WebJobs;
using System.Collections.Generic;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {

    public static class PubSubTrigger3 {

        [FunctionName("PubSubTrigger3")]
        public static void Run(
            [GooglePubSubTrigger("MyGooglePubSubConfig3")]
                IEnumerable<string> messages) {

            foreach (var message in messages) {
                System.Diagnostics.Debug.WriteLine(message);
            }

        }

    }
}
