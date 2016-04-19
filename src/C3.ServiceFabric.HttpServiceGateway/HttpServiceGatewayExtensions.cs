using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace C3.ServiceFabric.HttpServiceGateway
{
    /// <summary>
    /// Helper methods for adding one or more gateway instances to the request pipeline.
    /// </summary>
    public static class HttpServiceGatewayExtensions
    {
        /// <summary>
        /// Adds the <see cref="HttpServiceGatewayMiddleware"/> middleware to the request pipeline.
        /// This middleware is terminal.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <param name="pathMatch">The request path to match in the gateway.</param>
        /// <param name="serviceName">Name of the service within Service Fabric.</param>
        public static IApplicationBuilder RunHttpServiceGateway(this IApplicationBuilder app, PathString pathMatch, string serviceName)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            HttpServiceGatewayOptions options = new HttpServiceGatewayOptions { ServiceName = new Uri(serviceName) };

            return app.RunHttpServiceGateway(pathMatch, options);
        }

        /// <summary>
        /// Adds the <see cref="HttpServiceGatewayMiddleware"/> middleware to the request pipeline.
        /// This middleware is terminal.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <param name="pathMatch">The request path to match in the gateway.</param>
        /// <param name="options">Options for the <see cref="HttpServiceGatewayMiddleware" />.</param>
        public static IApplicationBuilder RunHttpServiceGateway(this IApplicationBuilder app, PathString pathMatch, HttpServiceGatewayOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            app.Map(pathMatch, x =>
            {
                x.RunHttpServiceGateway(options);
            });

            return app;
        }

        /// <summary>
        /// Adds the <see cref="HttpServiceGatewayMiddleware"/> middleware to the request pipeline.
        /// This middleware is terminal.
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <param name="serviceName">Name of the service within Service Fabric.</param>
        public static IApplicationBuilder RunHttpServiceGateway(this IApplicationBuilder app, string serviceName)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(app));

            HttpServiceGatewayOptions options = new HttpServiceGatewayOptions { ServiceName = new Uri(serviceName) };

            return app.RunHttpServiceGateway(options);
        }

        /// <summary>
        /// Adds the <see cref="HttpServiceGatewayMiddleware"/> middleware to the request pipeline.
        /// This middleware is terminal.
        /// </summary>
        /// <param name="options">Options for the <see cref="HttpServiceGatewayMiddleware" />.</param>
        public static IApplicationBuilder RunHttpServiceGateway(this IApplicationBuilder app, HttpServiceGatewayOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return app.UseMiddleware<HttpServiceGatewayMiddleware>(Options.Create(options));
        }
        
        /// <summary>
        /// Adds a gateway middleware for every service specified in the given configuration section.
        /// </summary>
        public static IApplicationBuilder RunHttpServiceGateways(this IApplicationBuilder app, IConfigurationSection config)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var services = new List<HttpServiceGatewayConfigurationEntry>();
            ConfigurationBinder.Bind(config, services);

            return RunHttpServiceGateways(app, services);
        }

        /// <summary>
        /// Adds a gateway middleware for every service specified in <paramref name="services"/>.
        /// </summary>
        public static IApplicationBuilder RunHttpServiceGateways(
            this IApplicationBuilder app,
            IEnumerable<HttpServiceGatewayConfigurationEntry> services)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            if (services == null)
                throw new ArgumentNullException(nameof(services));

            foreach (var configEntry in services)
            {
                app.RunHttpServiceGateway(configEntry.PathMatch, new HttpServiceGatewayOptions
                {
                    ServiceName = new Uri(configEntry.ServiceName),
                    ListenerName = configEntry.ListenerName
                });
            }

            return app;
        }
    }
}