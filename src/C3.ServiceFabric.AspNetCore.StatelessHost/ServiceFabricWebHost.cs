using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

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
            StartAsync().GetAwaiter().GetResult();
        }

        public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Console.WriteLine("ServiceFabricWebHost: StartAsync");

            string serviceTypeName = AspNetCoreService.GetServiceTypeName();

            _fabricRuntime.RegisterStatelessServiceFactory(serviceTypeName, new AspNetCoreServiceFactory(_webHost));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Console.WriteLine("ServiceFabricWebHost: StopAsync");

            return _webHost.StopAsync(cancellationToken);
        }

        public void Dispose()
        {
            Console.WriteLine("ServiceFabricWebHost: Dispose");

            _webHost.Dispose();
        }

    }
}