using System;
using Microsoft.AspNetCore.Http;
using Microsoft.ServiceFabric.Services.Client;

namespace C3.ServiceFabric.HttpServiceGateway
{
    /// <summary>
    /// Options for the HttpServiceGateway middleware.
    /// </summary>
    public class HttpServiceGatewayOptions
    {
        /// <summary>
        /// Name of the service within Service Fabric.
        /// </summary>
        public Uri ServiceName { get; set; }

        /// <summary>
        /// Name of the listener in the replica or instance to which the client should connect.
        /// </summary>
        public string ListenerName { get; set; }

        /// <summary>
        /// Delegate for resolving the partition key for partitioned services.
        /// </summary>
        public Func<HttpContext, ServicePartitionKey> ServicePartitionKeyResolver { get; set; }
    }
}