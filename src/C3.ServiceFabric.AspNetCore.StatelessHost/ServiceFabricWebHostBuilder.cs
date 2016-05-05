using System;
using System.Fabric;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        public IWebHostBuilder UseSetting(string key, string value)
        {
            _builder.UseSetting(key, value);
            return this;
        }

        public IWebHostBuilder UseLoggerFactory(ILoggerFactory loggerFactory)
        {
            _builder.UseLoggerFactory(loggerFactory);
            return this;
        }

        public IWebHostBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging)
        {
            _builder.ConfigureLogging(configureLogging);
            return this;
        }

        public IWebHostBuilder UseStartup(Type startupType)
        {
            // TODO @cweiss Remove in RC2 ( https://github.com/aspnet/Hosting/commit/8f5f8d28d00468725d9fd8dd95123f43d22f2c3c )
            _builder.UseStartup(startupType);
            return this;
        }

        public IWebHostBuilder Configure(Action<IApplicationBuilder> configureApplication)
        {
            // TODO @cweiss Remove in RC2 ( https://github.com/aspnet/Hosting/commit/8f5f8d28d00468725d9fd8dd95123f43d22f2c3c )
            _builder.Configure(configureApplication);
            return this;
        }
    }
}