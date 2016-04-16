using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Fabric;

namespace C3.ServiceFabric.AspNetCore.Hosting
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseServiceFabric(this IWebHostBuilder webHostBuilder)
        {
            Console.WriteLine("UseServiceFabric");

            if (webHostBuilder == null)
                throw new ArgumentNullException(nameof(webHostBuilder));

            IApplicationEnvironment application = PlatformServices.Default.Application;
            string serviceTypeName = AspNetCoreService.GetServiceTypeName(application);

            Console.WriteLine("UseServiceFabric - ServiceTypeName: " + serviceTypeName);
            Console.WriteLine("UseServiceFabric - ServerUrls: " + webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey));

            if (webHostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey) == null)
            {
                string endpointName = webHostBuilder.GetSetting("fabric.endpoint") ?? string.Format("{0}Endpoint", serviceTypeName);
                var endpoint = FabricRuntime.GetActivationContext().GetEndpoint(endpointName);

                string url = $"{endpoint.Protocol}://{FabricRuntime.GetNodeContext().IPAddressOrFQDN}:{endpoint.Port}";

                Console.WriteLine("UseServiceFabric - Assigning SF Url: " + url);

                webHostBuilder.UseUrls(url);
            }

            return webHostBuilder;
        }
    }
}