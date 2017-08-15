using System;
using System.Fabric;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace C3.ServiceFabric.AspNetCore.StatelessHost
{
    /// <summary>
    /// Provides a facade to the regular <see cref="WebHostBuilder"/> that allows 
    /// to optionally register the application in the current Service Fabric cluster.
    /// </summary>
    public class ServiceFabricWebHostBuilder : IWebHostBuilder, IDisposable
    {
        private readonly IWebHostBuilder _builder;
        private readonly FabricRuntime _fabricRuntime;

        public bool RunInServiceFabric { get; }

        public ServiceFabricWebHostBuilder(string[] args)
        {
            Console.WriteLine("ServiceFabricWebHostBuilder: Constructor");

            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            _builder = new WebHostBuilder()
                .UseConfiguration(config);

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

        public void Dispose()
        {
            _fabricRuntime?.Dispose();
        }

        public IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            _builder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
        {
            _builder.ConfigureServices(configureServices);
            return this;
        }

        public IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices)
        {
            _builder.ConfigureServices(configureServices);
            return this;
        }

        public string GetSetting(string key)
        {
            return _builder.GetSetting(key);
        }

        public IWebHostBuilder UseSetting(string key, string value)
        {
            _builder.UseSetting(key, value);
            return this;
        }
    }
}