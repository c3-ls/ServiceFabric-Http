using C3.ServiceFabric.HttpCommunication;
using Microsoft.ServiceFabric.Services.Client;
using System;

namespace Microsoft.Extensions.DependencyInjection
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

            // in case they haven't been added yet.
            // TODO this can be removed in RC2: https://github.com/aspnet/Hosting/issues/547
            services.AddOptions();

            services.AddTransient(x => ServicePartitionResolver.GetDefault());
            services.AddSingleton<IHttpCommunicationClientFactory, HttpCommunicationClientFactory>();

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