using Microsoft.AspNet.Builder;
using System;

namespace C3.ServiceFabric.HttpServiceGateway
{
    public static class HttpServiceGatewayExtensions
    {
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