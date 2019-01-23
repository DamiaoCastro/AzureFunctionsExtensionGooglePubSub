using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using AzureFunctions.Extensions.GooglePubSub.Config;

[assembly: WebJobsStartup(typeof(GooglePubSubWebJobsStartup), "GooglePubSub")]
namespace AzureFunctions.Extensions.GooglePubSub.Config
{
    public class GooglePubSubWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.UseGooglePubSub();
        }
    }
}