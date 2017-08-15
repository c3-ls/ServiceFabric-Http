using System.IO;
using C3.ServiceFabric.AspNetCore.StatelessHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HttpService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var builder = new ServiceFabricWebHostBuilder(args))
            {
                builder
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .ConfigureAppConfiguration((hostingContext, configBuilder) =>
                    {
                        configBuilder.AddJsonFile("appsettings.json");
                    })
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                        logging.AddConsole();
                    })
                    .ConfigureServices(services =>
                    {
                        services.AddMvc();
                    })
                    .Configure(app =>
                    {
                        app.UseStaticFiles();

                        app.UseMvc();
                    })
                    .Build()
                    .Run();
            }
        }
    }
}