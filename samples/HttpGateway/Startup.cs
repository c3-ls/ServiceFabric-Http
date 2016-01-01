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

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            // TODO - must be changed in a production app
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
            // define one Map(..) entry per service.

            app.Map("/service", appBuilder =>
            {
                appBuilder.RunHttpServiceGateway(new HttpServiceGatewayOptions
                {
                    ServiceName = new Uri("fabric:/GatewaySample/HttpServiceService")
                });
            });

            //app.Map("/someOtherService", appBuilder =>
            //{
            //    appBuilder.RunHttpServiceGateway(new HttpServiceGatewayOptions
            //    {
            //        ServiceName = new Uri("fabric:/GatewaySample/MySecondService")
            //    });
            //});
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}