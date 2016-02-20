using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using System;

namespace C3.ServiceFabric.HttpServiceGateway
{
    public static class HttpServiceGatewayExtensions
    {
        /// <summary>
        /// Adds the <see cref="HttpServiceGatewayMiddleware"/> middleware to the request pipeline.
        /// This middleware is terminal.
        /// </summary>
        public static IApplicationBuilder RunHttpServiceGateway(this IApplicationBuilder app, PathString pathMatch, HttpServiceGatewayOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

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
        public static IApplicationBuilder RunHttpServiceGateway(this IApplicationBuilder app, HttpServiceGatewayOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<HttpServiceGatewayMiddleware>(options);
        }
    }
}