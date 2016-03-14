using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;

namespace C3.ServiceFabric.AspNetCore.Hosting
{
    public class AspNetCoreService : IStatelessServiceInstance
    {
        private string _url;

        private readonly IConfiguration _configuration;

        private IWebHost _host;

        public AspNetCoreService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Initialize(StatelessServiceInitializationParameters initializationParameters)
        {
            try
            {
                IApplicationEnvironment application = PlatformServices.Default.Application;
                if (_configuration["server.urls"] == null)
                {
                    string endpointName = _configuration["fabric.endpoint"] ?? string.Format("{0}Endpoint", GetServiceTypeName(_configuration, application));
                    EndpointResourceDescription endpoint = initializationParameters.CodePackageActivationContext.GetEndpoint(endpointName);

                    _url = $"{endpoint.Protocol}://{FabricRuntime.GetNodeContext().IPAddressOrFQDN}:{endpoint.Port}";
                    _configuration["server.urls"] = _url;
                }
                else
                {
                    _url = _configuration["server.urls"];
                }

                Console.WriteLine("Initializing URL: " + _url);

                var webHostBuilder = new WebHostBuilder()
                    .UseConfiguration(_configuration)
                    .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                    .UseStartup(application.ApplicationName)
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<FabricClient>();
                    });

                _host = webHostBuilder.Build();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.ToString(), EventLogEntryType.Error);
                Console.WriteLine(ex.ToString());

                throw;
            }
        }

        public Task<string> OpenAsync(IStatelessServicePartition partition, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine(string.Format("Starting on URL: {0}", _url));
                EventLog.WriteEntry("Application", string.Format("Starting on URL: {0}", _url), EventLogEntryType.Information);

                Start();

                return Task.FromResult(_url);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.ToString(), EventLogEntryType.Error);
                Console.WriteLine(ex.ToString());

                Stop();
            }
            return null;
        }

        public void Start()
        {
            if (_host == null)
            {
                EventLog.WriteEntry("Application", "_hostingEngine is null", EventLogEntryType.Error);
                return;
            }

            _host.Start();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            Stop();
            return Task.FromResult(true);
        }

        public void Stop()
        {
            _host?.Dispose();
        }

        public void Abort()
        {
            Stop();
        }

        public static string GetServiceTypeName(IConfiguration config, IApplicationEnvironment appEnv)
        {
            string serviceTypeName;
            if ((serviceTypeName = config["serviceType"]) == null)
            {
                serviceTypeName = string.Format("{0}Type", config["app"] ?? appEnv.ApplicationName);
            }
            return serviceTypeName;
        }
    }
}