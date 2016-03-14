using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace C3.ServiceFabric.HttpServiceGateway
{
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

            // TODO - RC2: change to Options.Create(options)
            return app.UseMiddleware<HttpServiceGatewayMiddleware>(options);
        }
    }
}