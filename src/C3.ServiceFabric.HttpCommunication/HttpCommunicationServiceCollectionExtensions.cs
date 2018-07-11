using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace C3.ServiceFabric.HttpCommunication
{
    public static class HttpCommunicationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds services required for communicating with HTTP based Service Fabric services.
        /// </summary>
        public static IServiceCollection AddServiceFabricHttpCommunication(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddTransient(x => ServicePartitionResolver.GetDefault());
            services.AddTransient<IExceptionHandler, HttpCommunicationExceptionHandler>();
            services.AddScoped<IHttpCommunicationClientFactory, HttpCommunicationClientFactory>();

            return services;
        }

        /// <summary>
        /// Adds services required for communicating with HTTP based Service Fabric services.
        /// </summary>
        public static IServiceCollection AddServiceFabricHttpCommunication(this IServiceCollection services, Action<HttpCommunicationOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.Configure(configure);

            return services.AddServiceFabricHttpCommunication();
        }
    }
}