using C3.ServiceFabric.HttpCommunication;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace HttpDirect
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; set; }

        public Startup(ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Verbose);

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // use default options ...

            //services.AddServiceFabricHttpCommunication();

            // ... or overwrite certain defaults from Configuration system ...

            //services.Configure<HttpCommunicationOptions>(Configuration.GetSection("HttpCommunication"));
            //services.AddServiceFabricHttpCommunication();

            // ... or overwrite certain defaults with code ...

            //services.AddServiceFabricHttpCommunication(options =>
            //{
            //    options.MaxRetryCount = 2;
            //});

            // ... or combine Configuration system with custom settings
            services.Configure<HttpCommunicationOptions>(Configuration.GetSection("HttpCommunication"));
            services.AddServiceFabricHttpCommunication(options =>
            {
                options.DoNotRetryExceptionTypes = new[] { typeof(NotSupportedException) };
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            app.Map("/call", appBuilder =>
            {
                // check the Middleware for how to call the service.
                appBuilder.UseMiddleware<DirectCommunicationMiddleware>();
            });

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}