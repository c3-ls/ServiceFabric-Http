using C3.ServiceFabric.HttpCommunication;
using C3.ServiceFabric.HttpServiceGateway;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace HttpGateway
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup(ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Verbose);

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

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
            // define one entry per service.

            app.RunHttpServiceGateway("/service", new HttpServiceGatewayOptions
            {
                ServiceName = new Uri("fabric:/GatewaySample/HttpServiceService")
            });

            // if you need to do multiple things within the path branch, you can use app.Map():
            //app.Map("/service", appBuilder =>
            //{
            //    appBuilder.RunHttpServiceGateway(new HttpServiceGatewayOptions
            //    {
            //        ServiceName = new Uri("fabric:/GatewaySample/HttpServiceService")
            //    });
            //});
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}