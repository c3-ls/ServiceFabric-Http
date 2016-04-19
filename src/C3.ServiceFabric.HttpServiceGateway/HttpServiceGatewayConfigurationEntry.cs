namespace C3.ServiceFabric.HttpServiceGateway
{
    /// <summary>
    /// Represents one entry in a <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>-based
    /// configuration of the gateway.
    /// </summary>
    public class HttpServiceGatewayConfigurationEntry
    {
        /// <summary>
        /// The path on the gateway that should be used for forwarding requests to the service.
        /// </summary>
        public string PathMatch { get; set; }

        /// <summary>
        /// Name of the service within Service Fabric.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Name of the listener in the replica or instance to which the client should connect.
        /// </summary>
        public string ListenerName { get; set; }

        public HttpServiceGatewayConfigurationEntry()
        {
        }

        public HttpServiceGatewayConfigurationEntry(string pathMatch, string serviceName, string listenerName = null)
        {
            PathMatch = pathMatch;
            ServiceName = serviceName;
            ListenerName = listenerName;
        }
    }
}