using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Fabric;

namespace C3.ServiceFabric.AspNetCore.StatelessHost
{
    public class ServiceFabricWebHost : IWebHost
    {
        private readonly IWebHost _webHost;
        private readonly FabricRuntime _fabricRuntime;

        public ServiceFabricWebHost(IWebHost webHost, FabricRuntime fabricRuntime)
        {
            Console.WriteLine("ServiceFabricWebHost: Constructor");

            if (webHost == null)
                throw new ArgumentNullException(nameof(webHost));

            if (fabricRuntime == null)
                throw new ArgumentNullException(nameof(fabricRuntime));

            _webHost = webHost;
            _fabricRuntime = fabricRuntime;
        }

        public IFeatureCollection ServerFeatures => _webHost.ServerFeatures;

        public IServiceProvider Services => _webHost.Services;

        public void Start()
        {
            Console.WriteLine("ServiceFabricWebHost: Start");

            var appEnv = PlatformServices.Default.Application;

            string serviceTypeName = AspNetCoreService.GetServiceTypeName(appEnv);

            _fabricRuntime.RegisterStatelessServiceFactory(serviceTypeName, new AspNetCoreServiceFactory(_webHost));
        }

        public void Dispose()
        {
            Console.WriteLine("ServiceFabricWebHost: Dispose");

            _webHost.Dispose();
        }
    }
}