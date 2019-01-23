namespace AzureFunctions.Extensions.GooglePubSub.Services {
    internal interface IServiceFactory {

        T GetService<T>(GooglePubSubBaseAttribute googlePubSubAttribute) where T : class;

    }
}
