using Microsoft.AspNetCore.Http;
using System;

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
        /// Delegate for resolving Int64 partitioned services.
        /// </summary>
        public Func<HttpContext, long> Int64PartitionKeyResolver { get; set; }

        /// <summary>
        /// Delegate for resolving string partitioned services.
        /// </summary>
        public Func<HttpContext, string> NamedPartitionKeyResolver { get; set; }
    }
}