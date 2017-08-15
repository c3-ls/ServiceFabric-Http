using C3.ServiceFabric.HttpCommunication;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HttpDirect
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder()
                .ConfigureServices((hostingContext, services) =>
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
                    services.Configure<HttpCommunicationOptions>(hostingContext.Configuration.GetSection("HttpCommunication"));
                    services.AddServiceFabricHttpCommunication(options =>
                    {
                        options.RetryHttpStatusCodeErrors = true;
                    });
                })
                .Configure(app =>
                {
                    app.UseDeveloperExceptionPage();

                    app.Map("/call", appBuilder =>
                    {
                        // check the Middleware for how to call the service.
                        appBuilder.UseMiddleware<DirectCommunicationMiddleware>();
                    });

                    app.Run(async context =>
                    {
                        await context.Response.WriteAsync("Hello World!");
                    });
                })
                .Build();

            builder.Run();
        }
    }
}