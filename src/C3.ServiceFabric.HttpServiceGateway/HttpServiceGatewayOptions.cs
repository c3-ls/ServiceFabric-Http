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
        /// Exception types which should not result in a retry operation.
        /// </summary>
        public Type[] DoNotRetryExceptionTypes { get; set; }

        /// <summary>
        /// The number of times a service request is retried in case of an error.
        /// </summary>
        public int MaxRetryCount { get; set; } = GlobalConfig.DefaultMaxRetryCount;

        /// <summary>
        /// The maximum amount of time to wait for one single service request. (this value is reset for every retry)
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = GlobalConfig.DefaultOperationTimeout;
    }
}