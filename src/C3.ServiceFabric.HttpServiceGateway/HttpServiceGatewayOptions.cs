using Microsoft.AspNet.Http;
using System;

namespace C3.ServiceFabric.HttpServiceGateway
{
    public class HttpServiceGatewayOptions
    {
        /// <summary>
        /// Name of the service within Service Fabric.
        /// </summary>
        public Uri ServiceName { get; set; }

        /// <summary>
        /// Delegate for resolving Int64 partitioned services.
        /// </summary>
        public Func<HttpContext, long> Int64PartitionKeyResolver { get; set; }

        /// <summary>
        /// Delegate for resolving string partitioned services.
        /// </summary>
        public Func<HttpContext, string> NamedPartitionKeyResolver { get; set; }

        /// <summary>
        /// Configuration options for the retry behavior of the HttpClient.
        /// </summary>
        public HttpCommunicationClientOptions HttpCommunication { get; set; } = new HttpCommunicationClientOptions();
    }
}