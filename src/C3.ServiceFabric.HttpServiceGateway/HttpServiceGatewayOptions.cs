using System;
using Microsoft.AspNetCore.Http;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace C3.ServiceFabric.HttpServiceGateway
{
    /// <summary>
    /// Options for a single gateway instance.
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

        /// <summary>
        /// Defines which replica should be selected. Defaults to <see cref="Microsoft.ServiceFabric.Services.Communication.Client.TargetReplicaSelector.RandomInstance"/>.
        /// </summary>
        public TargetReplicaSelector TargetReplicaSelector { get; set; } = TargetReplicaSelector.RandomInstance;

        /// <summary>
        /// Defines the retry behavior of the <see cref="ServicePartitionClient{TCommunicationClient}"/>.
        /// </summary>
        public OperationRetrySettings RetrySettings { get; set; } = new OperationRetrySettings();
    }
}