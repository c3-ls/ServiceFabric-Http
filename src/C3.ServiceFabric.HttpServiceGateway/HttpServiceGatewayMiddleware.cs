using C3.ServiceFabric.HttpCommunication;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace C3.ServiceFabric.HttpServiceGateway
{
    /// <summary>
    /// Proxy-Middleware for ServiceFabric services with a HTTP endpoint.
    /// </summary>
    public class HttpServiceGatewayMiddleware
    {
        private readonly HttpServiceGatewayOptions _options;
        private readonly ILogger _logger;
        private readonly IHttpCommunicationClientFactory _httpCommunicationClientFactory;

        public HttpServiceGatewayMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            HttpServiceGatewayOptions gatewayOptions,
            IHttpCommunicationClientFactory httpCommunicationClientFactory)
        {
            // TODO - RC2: change parameter to IOptions<HttpServiceGatewayOptions>

            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (gatewayOptions == null)
                throw new ArgumentNullException(nameof(gatewayOptions));

            if (httpCommunicationClientFactory == null)
                throw new ArgumentNullException(nameof(httpCommunicationClientFactory));
            
            if (gatewayOptions.ServiceName == null)
                throw new ArgumentNullException("options.ServiceFabricUri");
            
            if (gatewayOptions.NamedPartitionKeyResolver != null && gatewayOptions.Int64PartitionKeyResolver != null)
                throw new ArgumentException("Only one PartitionKey-Resolver may be set.");

            // "next" is not stored because this is a terminal middleware
            _logger = loggerFactory.CreateLogger(HttpServiceGatewayDefaults.LoggerName);
            _options = gatewayOptions;
            _httpCommunicationClientFactory = httpCommunicationClientFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            byte[] contextRequestBody = null;

            try
            {
                ServicePartitionClient<HttpCommunicationClient> servicePartitionClient = CreateServicePartitionClient(context);

                // Request Body is a forward-only stream so it is read into memory for potential retries.
                // NOTE: This might be an issue for very big requests.
                if (context.Request.ContentLength > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        context.Request.Body.CopyTo(memoryStream);
                        contextRequestBody = memoryStream.ToArray();
                    }
                }

                HttpResponseMessage response = await servicePartitionClient.InvokeWithRetryAsync(
                    client => ExecuteServiceCallAsync(client, context, contextRequestBody));

                await response.CopyToCurrentContext(context);
            }
            catch (HttpResponseException ex)
            {
                // as soon as we get a response from the service, we don't treat it as an error from the gateway.
                // For this reason, we forward faulty responses to the caller 1:1.
                _logger.LogWarning("Service returned non retryable error. Reason: {Reason}", "HTTP " + ex.Response.StatusCode);
                await ex.Response.CopyToCurrentContext(context);
            }
        }

        private async Task<HttpResponseMessage> ExecuteServiceCallAsync(HttpCommunicationClient client, HttpContext context, byte[] contextRequestBody)
        {
            // create request and copy all details

            HttpRequestMessage req = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = new Uri(context.Request.Path + context.Request.QueryString, UriKind.Relative)
            };

            if (contextRequestBody != null)
            {
                req.Content = new ByteArrayContent(contextRequestBody);
            }

            req.CopyHeadersFromCurrentContext(context);
            req.AddProxyHeaders(context);

            // execute request

            HttpResponseMessage response = await client.HttpClient.SendAsync(req, context.RequestAborted);

            // cases in which we want to invoke the retry logic from the ClientFactory
            int statusCode = (int)response.StatusCode;
            if ((statusCode >= 500 && statusCode < 600) || statusCode == (int)HttpStatusCode.NotFound)
            {
                throw new HttpResponseException("Service call failed", response);
            }

            return response;
        }

        private ServicePartitionClient<HttpCommunicationClient> CreateServicePartitionClient(HttpContext context)
        {
            // these different calls are required because every constructor sets different variables internally.

            ServicePartitionClient<HttpCommunicationClient> servicePartitionClient = null;

            if (_options.NamedPartitionKeyResolver != null)
            {
                servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                    _httpCommunicationClientFactory, _options.ServiceName, _options.NamedPartitionKeyResolver(context));
            }
            else if (_options.Int64PartitionKeyResolver != null)
            {
                servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                    _httpCommunicationClientFactory, _options.ServiceName, _options.Int64PartitionKeyResolver(context));
            }
            else
            {
                servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                    _httpCommunicationClientFactory, _options.ServiceName);
            }

            return servicePartitionClient;
        }
    }
}