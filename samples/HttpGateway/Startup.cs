using System.IO;
using System.Net;
using C3.ServiceFabric.AspNetCore.StatelessHost;
using C3.ServiceFabric.HttpCommunication;
using C3.ServiceFabric.HttpServiceGateway;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Collections.Generic;

namespace HttpGateway
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup(ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            loggerFactory.AddConsole(new ConsoleLoggerSettings
            {
                Switches =
                {
                    ["Default"] = LogLevel.Debug,
                    ["Microsoft"] = LogLevel.Information
                }
            });

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // this adds the required services
            services.Configure<HttpCommunicationOptions>(Configuration.GetSection("HttpCommunication"));
            services.AddServiceFabricHttpCommunication();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            // must be changed in a production app
            app.UseDeveloperExceptionPage();

            ConfigureHttpServiceGateways(app);

            // catch-all
            app.Run(async context =>
            {
                var logger = loggerFactory.CreateLogger("Catch-All");
                logger.LogWarning("No endpoint found for request {path}", context.Request.Path + context.Request.QueryString);

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync("Not Found");
            });
        }

        /// <summary>
        /// Configures a gateway endpoint for every service that should be exposed.
        /// </summary>
        private void ConfigureHttpServiceGateways(IApplicationBuilder app)
        {
            // ---------------------
            // Samples for configuring a single gateway instance

            // this would forward every request to the service. this way, your application can only handle one service.

            //app.RunHttpServiceGateway("fabric:/GatewaySample/HttpService");

            // ... this only forwards requests on a certain path. This is the simplest case for non-partitioned services.

            app.RunHttpServiceGateway("/service1", "fabric:/GatewaySample/HttpService");

            // ... pass an instance of HttpServiceGatewayOptions for more options (e.g. to define the ServicePartitionKeyResolver)

            //app.RunHttpServiceGateway("/service", new HttpServiceGatewayOptions
            //{
            //    ServiceName = new Uri("fabric:/GatewaySample/HttpService"),
            //    ServicePartitionKeyResolver = (context) =>
            //    {
            //        string namedPartitionKey = context.Request.Query["partitionKey"];
            //        return new ServicePartitionKey(namedPartitionKey);
            //    }
            //});

            // ... if you need to do multiple things within the path branch, you can use app.Map():

            //app.Map("/service", appBuilder =>
            //{
            //    appBuilder.RunHttpServiceGateway(new HttpServiceGatewayOptions
            //    {
            //        ServiceName = new Uri("fabric:/GatewaySample/HttpService")
            //    });
            //});

            // ---------------------
            // Samples for configuring many gateway instances

            // this configures one or many gateway instances based on the Configuration system (e.g. file-based).
            app.RunHttpServiceGateways(Configuration.GetSection("GatewayServices"));

            // this configures one or many gateway instances based on a list of configuration entries.
            var services = new List<HttpServiceGatewayConfigurationEntry>
            {
                new HttpServiceGatewayConfigurationEntry("/another-service1", "fabric:/MyApp/AnotherService1"),
                new HttpServiceGatewayConfigurationEntry("/another-service2", "fabric:/MyApp/AnotherService2", "OwinListener")
            };
            app.RunHttpServiceGateways(services);
        }

        public static void Main(string[] args)
        {
            using (var builder = new ServiceFabricWebHostBuilder(args))
            {
                builder
                     .UseKestrel()
                     .UseContentRoot(Directory.GetCurrentDirectory())
                     .UseStartup<Startup>()
                     .Build()
                     .Run();
            }
        }
    }
}