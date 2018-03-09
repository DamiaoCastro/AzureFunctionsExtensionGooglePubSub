using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {

    public static class PubSubTrigger2 {

        [FunctionName("PubSubTrigger2")]
        public static void Run(
            [GooglePubSubTrigger("MyGooglePubSubConfig2")] IEnumerable<string> messages,
           [GooglePubSub("MyGooglePubSubConfig2")] ICollector<string> messagesCollector,
           TraceWriter traceWriter
            ) {

            foreach (var message in messages) {
                traceWriter.Info(message);
            }

            //PubSubCollector1.Run(null, messagesCollector);

        }

    }
}
