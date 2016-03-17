using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Fabric;

namespace C3.ServiceFabric.AspNetCore.Hosting
{
    public class ServiceFabricWebHostBuilder : IWebHostBuilder, IDisposable
    {
        private readonly IWebHostBuilder _builder;
        private readonly FabricRuntime _fabricRuntime;

        public bool RunInServiceFabric { get; }

        public ServiceFabricWebHostBuilder(string[] args)
        {
            Console.WriteLine("ServiceFabricWebHostBuilder: Constructor");

            _builder = new WebHostBuilder()
                .UseDefaultConfiguration(args);

            RunInServiceFabric = string.Equals(_builder.GetSetting("fabric"), "true", StringComparison.OrdinalIgnoreCase);

            Console.WriteLine("RunInServiceFabric: " + RunInServiceFabric);

            if (RunInServiceFabric)
            {
                _fabricRuntime = FabricRuntime.Create();
                Console.WriteLine("FabricRuntime initialized");
            }
        }

        public IWebHost Build()
        {
            Console.WriteLine("ServiceFabricWebHostBuilder: Build");

            if (!RunInServiceFabric)
            {
                Console.WriteLine("ServiceFabricWebHostBuilder.Build -> returning regular host");
                return _builder.Build();
            }

            // The host should run in Service Fabric

            Console.WriteLine("ServiceFabricWebHostBuilder -> using SF");
            _builder.UseServiceFabric();
            return new ServiceFabricWebHost(_builder.Build(), _fabricRuntime);
        }

        public IWebHostBuilder Configure(Action<IApplicationBuilder> configureApplication)
        {
            _builder.Configure(configureApplication);
            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            _builder.ConfigureServices(configureServices);
            return this;
        }

        public void Dispose()
        {
            _fabricRuntime?.Dispose();
        }

        public string GetSetting(string key)
        {
            return _builder.GetSetting(key);
        }

        public IWebHostBuilder UseServer(IServerFactory factory)
        {
            _builder.UseServer(factory);
            return this;
        }

        public IWebHostBuilder UseSetting(string key, string value)
        {
            _builder.UseSetting(key, value);
            return this;
        }

        public IWebHostBuilder UseStartup(Type startupType)
        {
            _builder.UseStartup(startupType);
            return this;
        }
    }
}