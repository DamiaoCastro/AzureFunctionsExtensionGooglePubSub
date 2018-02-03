namespace AzureFunctions.Extensions.GooglePubSub.PubSub {

    internal class CreateSubscriptionRequest {
        public int ackDeadlineSeconds { get; set; }
        public string messageRetentionDuration { get; set; }
        public string name { get; set; }
        public Pushconfig pushConfig { get; set; }
        public bool retainAckedMessages { get; set; }
        public string topic { get; set; }
    }

    internal class Pushconfig {
        //public Attributes attributes { get; set; }
        public string pushEndpoint { get; set; }
    }

    //public class Attributes {
    //}

}