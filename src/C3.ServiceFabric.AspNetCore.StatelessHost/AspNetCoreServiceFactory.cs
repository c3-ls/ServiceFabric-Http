using Microsoft.AspNetCore.Hosting;
using System;
using System.Fabric;

namespace C3.ServiceFabric.AspNetCore.StatelessHost
{
    public class AspNetCoreServiceFactory : IStatelessServiceFactory
    {
        private readonly IWebHost _webHost;

        public AspNetCoreServiceFactory(IWebHost webHost)
        {
            _webHost = webHost;
        }

        public IStatelessServiceInstance CreateInstance(string serviceTypeName, Uri serviceName, byte[] initializationData, Guid partitionId, long instanceId)
        {
            return new AspNetCoreService(_webHost);
        }
    }
}