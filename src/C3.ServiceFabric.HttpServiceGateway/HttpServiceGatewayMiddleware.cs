using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using C3.ServiceFabric.HttpCommunication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace C3.ServiceFabric.HttpServiceGateway
{
    /// <summary>
    /// Proxy-Middleware for ServiceFabric services with a HTTP endpoint.
    /// </summary>
    public class HttpServiceGatewayMiddleware
    {
        private readonly ILogger _logger;
        private readonly IHttpCommunicationClientFactory _httpCommunicationClientFactory;
        private readonly HttpServiceGatewayOptions _gatewayOptions;

        public HttpServiceGatewayMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            IHttpCommunicationClientFactory httpCommunicationClientFactory,
            IOptions<HttpServiceGatewayOptions> gatewayOptions)
        {
            if (next == null)
                throw new ArgumentNullException(nameof(next));

            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            if (httpCommunicationClientFactory == null)
                throw new ArgumentNullException(nameof(httpCommunicationClientFactory));

            if (gatewayOptions?.Value == null)
                throw new ArgumentNullException(nameof(gatewayOptions));

            if (gatewayOptions.Value.ServiceName == null)
                throw new ArgumentNullException($"{nameof(gatewayOptions)}.{nameof(gatewayOptions.Value.ServiceName)}");

            // "next" is not stored because this is a terminal middleware
            _logger = loggerFactory.CreateLogger(HttpServiceGatewayDefaults.LoggerName);
            _httpCommunicationClientFactory = httpCommunicationClientFactory;
            _gatewayOptions = gatewayOptions.Value;
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
                    client => ExecuteServiceCallAsync(client, context, contextRequestBody),
                    context.RequestAborted);

                if (response != null)
                {
                    await response.CopyToCurrentContext(context);
                }
                else
                {
                    _logger.LogWarning("No response. RequestAborted: {RequestAborted}", context.RequestAborted);
                }
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
            // We don't throw because this would result in a retry loop.
            if (context.RequestAborted.IsCancellationRequested)
                return null;

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

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(req, context.RequestAborted, _gatewayOptions.ShouldForwardCookies);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // We don't have to retry if the client is no longer connected.
                return null;
            }

            // cases in which we want to invoke the retry logic from the ClientFactory

            InvokeRetryIfNecessary(response);

            return response;
        }

        private void InvokeRetryIfNecessary(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Compatibility with the official reverse proxy: Retry all 404 unless there's a special header.
                // https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reverseproxy

                if (response.Headers.TryGetValues("X-ServiceFabric", out var val)
                    && string.Equals("ResourceNotFound", val.FirstOrDefault(), StringComparison.OrdinalIgnoreCase))
                {
                    response.Headers.Remove("X-ServiceFabric");
                    return;
                }

                throw new HttpResponseException("Resource not found", response);
            }

            int statusCode = (int) response.StatusCode;
            if (statusCode >= 500 && statusCode < 600)
            {
                throw new HttpResponseException("Service call failed", response);
            }
        }

        private ServicePartitionClient<HttpCommunicationClient> CreateServicePartitionClient(HttpContext context)
        {
            // these different calls are required because every constructor sets different variables internally.

            var servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                communicationClientFactory: _httpCommunicationClientFactory,
                serviceUri: _gatewayOptions.ServiceName,
                partitionKey: _gatewayOptions.ServicePartitionKeyResolver?.Invoke(context),
                listenerName: _gatewayOptions.ListenerName,
                targetReplicaSelector: _gatewayOptions.TargetReplicaSelector,
                retrySettings: _gatewayOptions.RetrySettings);

            return servicePartitionClient;
        }
    }
}