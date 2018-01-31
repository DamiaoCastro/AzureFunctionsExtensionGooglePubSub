using System;
using System.Runtime.Serialization;

namespace AzureFunctions.Extensions.GooglePubSub.PubSub
{
    [Serializable]
    internal class FailedToAcknowledgeException : Exception
    {
        public FailedToAcknowledgeException()
        {
        }

        public FailedToAcknowledgeException(string message) : base(message)
        {
        }

        public FailedToAcknowledgeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FailedToAcknowledgeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}