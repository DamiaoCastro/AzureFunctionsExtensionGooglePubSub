using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;

namespace AzureFunctions.Extensions.GooglePubSub.DemoProject2 {

    public static class PubSubTrigger2 {

        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("PubSubTrigger2")]
        public static void Run(
            [GooglePubSubTrigger("MyGooglePubSubConfig2")] IEnumerable<string> messages,
            [GooglePubSub("MyGooglePubSubConfig2")] ICollector<string> messagesCollector,
            TraceWriter traceWriter
            ) {

            //var list = new List<Task>();
            //foreach (var message in messages) {
            //    traceWriter.Info(message);
            //}

            //await Task.WhenAll(list);

            PubSubCollector1.Run(null, messagesCollector);
        }

    }
}
