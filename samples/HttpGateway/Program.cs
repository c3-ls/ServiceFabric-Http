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
    public class Program
    {
        /// <summary>
        /// Configures a gateway endpoint for every service that should be exposed.
        /// </summary>
        private static void ConfigureHttpServiceGateways(IApplicationBuilder app, WebHostBuilderContext hostingContext)
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
            app.RunHttpServiceGateways(hostingContext.Configuration.GetSection("GatewayServices"));

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
                     .ConfigureAppConfiguration((context, configBuilder) =>
                     {
                         configBuilder.AddJsonFile("appsettings.json");
                     })
                     .ConfigureLogging((hostingContext, logging) =>
                     {
                         logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                         logging.AddConsole();
                     })
                     .ConfigureServices((hostingContext, services) =>
                     {
                         // this adds the required services
                         services.Configure<HttpCommunicationOptions>(hostingContext.Configuration.GetSection("HttpCommunication"));
                         services.AddServiceFabricHttpCommunication();
                     })
                     .Configure(app =>
                     {
                         var hostingContext = app.ApplicationServices.GetRequiredService<WebHostBuilderContext>();

                         // must be changed in a production app
                         app.UseDeveloperExceptionPage();

                         ConfigureHttpServiceGateways(app, hostingContext);

                         // catch-all
                         app.Run(async context =>
                         {
                             var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                             var logger = loggerFactory.CreateLogger("Catch-All");

                             logger.LogWarning("No endpoint found for request {path}", context.Request.Path + context.Request.QueryString);

                             context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                             await context.Response.WriteAsync("Not Found");
                         });
                     })
                     .Build()
                     .Run();
            }
        }
    }
}