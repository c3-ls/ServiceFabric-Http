using Microsoft.AspNet.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;

namespace C3.ServiceFabric.HttpServiceGateway
{
    public static class HttpRequestMessageExtensions
    {
        public static class CustomHeaderNames
        {
            public const string Forwarded = "Forwarded";

            public const string XForwardedFor = "X-Forwarded-For";
            public const string XForwardedHost = "X-Forwarded-Host";
            public const string XForwardedProto = "X-Forwarded-Proto";

            public const string XForwardedPathBase = "X-Forwarded-PathBase";
        }

        /// <summary>
        /// Request headers which are either set by the framework or shouldn't be copied.
        /// </summary>
        private static List<string> _ignoredRequestHeaders = new List<string>
        {
            "Connection",
            "Content-Length",
            "Date",
            "Expect",

            // forwarding the original Host is not permitted by https://tools.ietf.org/html/rfc7230#section-5.4
            // However there are also recommendations against this:
            // https://docs.oracle.com/cd/E40519_01/studio.310/studio_install/src/cidi_studio_reverse_proxy_preserve_host_headers.html
            "Host",

            "If-Modified-Since",
            "Range",
            "Transfer-Encoding",
            "Proxy-Connection"
        };

        /// <summary>
        /// Copies all details from the current request to the target.
        /// </summary>
        public static void CopyFromCurrentContext(this HttpRequestMessage target, HttpContext context)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (context == null) throw new ArgumentNullException(nameof(context));

            // interesting links:
            // http://www.wiliam.com.au/wiliam-blog/web-design-sydney-relaying-an-httprequest-in-asp-net
            // https://github.com/aspnet/Proxy/blob/dev/src/Microsoft.AspNet.Proxy/ProxyMiddleware.cs

            target.Method = new HttpMethod(context.Request.Method);

            if (context.Request.ContentLength > 0)
            {
                target.Content = new StreamContent(context.Request.Body);
            }

            foreach (var header in context.Request.Headers)
            {
                if (_ignoredRequestHeaders.Contains(header.Key))
                    continue;

                if (!target.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && target.Content != null)
                {
                    target.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
                break;
            }

            AddProxyHeaders(target, context);
        }

        private static void AddProxyHeaders(HttpRequestMessage target, HttpContext context)
        {
            // Via (will be added to an existing Via header)
            target.Headers.Add(HeaderNames.Via, "1.1 " + Environment.MachineName);

            // proxy headers haven't been standardized until 2014 (https://tools.ietf.org/html/rfc7239)
            // Even now, this standard is not very common and most systems only support the non-standard headers.
            // For this reason, we support both ways.

            AddRfcForwardedHeader(target, context);

            AddNonStandardForwardedHeaders(target, context);

            // this is a custom solution for proxies who serve the origin on a different path level.
            // Usually, the origin shouldn't know about a proxy and therefore it's the responsibility of the proxy to modify
            // the response. However, with this header we give the origin the posibility to adopt its links directly.
            // (it's only added if there was no other previous proxy who set this to preserve the first one which is important for the user)
            if (context.Request.PathBase.HasValue && !target.Headers.Contains(CustomHeaderNames.XForwardedPathBase))
            {
                target.Headers.Add(CustomHeaderNames.XForwardedPathBase, context.Request.PathBase);
            }
        }

        /// <summary>
        /// Adds a "forwarded header according to https://tools.ietf.org/html/rfc7239
        /// </summary>
        private static void AddRfcForwardedHeader(HttpRequestMessage target, HttpContext context)
        {
            List<string> parts = new List<string>();

            // "by"
            parts.Add($"by=_{Environment.MachineName}");

            // "for"
            if (context.Connection.RemoteIpAddress != null)
            {
                if (context.Connection.RemoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    parts.Add($"for=\"[{context.Connection.RemoteIpAddress.ToString()}]\"");
                }
                else
                {
                    parts.Add($"for={context.Connection.RemoteIpAddress.ToString()}");
                }
            }

            // "host"
            if (context.Request.Host.HasValue)
            {
                parts.Add("host=" + context.Request.Host.Value);
            }

            // "proto"
            parts.Add("proto=" + context.Request.Scheme);

            // Join and add to target (will add an additional header if there already is one from a previous proxy)
            string forwardedHeaderValue = string.Join(";", parts);
            target.Headers.Add(CustomHeaderNames.Forwarded, forwardedHeaderValue);
        }

        private static void AddNonStandardForwardedHeaders(HttpRequestMessage target, HttpContext context)
        {
            // Some interesting links:
            // http://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/RequestAndResponseBehaviorCustomOrigin.html#RequestCustomIPAddresses
            // http://docs.aws.amazon.com/ElasticLoadBalancing/latest/DeveloperGuide/x-forwarded-headers.html

            // We only add these headers if they are not yet there to allow the origin to react based on the
            // original values from the client.

            if (!target.Headers.Contains(CustomHeaderNames.XForwardedHost))
            {
                target.Headers.Add(CustomHeaderNames.XForwardedHost, context.Request.Host.Value);
            }

            if (!target.Headers.Contains(CustomHeaderNames.XForwardedProto))
            {
                target.Headers.Add(CustomHeaderNames.XForwardedProto, context.Request.Scheme);
            }

            // X-Forwarded-For addresses have to be concatenated
            string remoteIpAddress = context.Connection.RemoteIpAddress?.ToString();
            string existingXForwardedFor = context.Request.Headers[CustomHeaderNames.XForwardedFor];
            if (existingXForwardedFor != null && remoteIpAddress != null)
            {
                target.Headers.Remove(CustomHeaderNames.XForwardedFor);
                target.Headers.Add(CustomHeaderNames.XForwardedFor, existingXForwardedFor + "," + remoteIpAddress);
            }
            else if (remoteIpAddress != null)
            {
                target.Headers.Add(CustomHeaderNames.XForwardedFor, remoteIpAddress);
            }
        }
    }
}