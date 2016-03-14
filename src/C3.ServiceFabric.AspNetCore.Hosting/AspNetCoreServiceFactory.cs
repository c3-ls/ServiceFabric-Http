using Microsoft.Extensions.Configuration;
using System;
using System.Fabric;

namespace C3.ServiceFabric.AspNetCore.Hosting
{
    public class AspNetCoreServiceFactory : IStatelessServiceFactory
    {
        private readonly IConfiguration _configuration;

        public AspNetCoreServiceFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IStatelessServiceInstance CreateInstance(string serviceTypeName, Uri serviceName, byte[] initializationData, Guid partitionId, long instanceId)
        {
            return new AspNetCoreService(_configuration);
        }
    }
}