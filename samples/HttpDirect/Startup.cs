using C3.ServiceFabric.HttpCommunication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
            loggerFactory.AddConsole(LogLevel.Debug);

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
                options.RetryHttpStatusCodeErrors = true;
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

        public static void Main(string[] args)
        {
            var builder = new WebHostBuilder()
                .UseDefaultConfiguration(args)
                .UseServer("Microsoft.AspNetCore.Server.Kestrel")
                .UseStartup<Startup>()
                .Build();

            builder.Run();
        }
    }
}