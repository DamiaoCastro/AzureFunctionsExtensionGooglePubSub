using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using AzureFunctions.Extensions.GooglePubSub;

[assembly: WebJobsStartup(typeof(PubSubWebJobsStartup), "PubSub")]

namespace AzureFunctions.Extensions.GooglePubSub
{
    public class PubSubWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.UseGooglePubSub();
        }
    }
}
