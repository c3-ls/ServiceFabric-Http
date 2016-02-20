using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace C3.ServiceFabric.HttpCommunication
{
    /// <summary>
    /// Factory that creates clients that know to communicate with the service.
    /// Contains a service partition resolver that resolves a partition key
    /// and sets BaseAddress to the address of the replica that should serve a request.
    /// </summary>
    public class HttpCommunicationClientFactory : CommunicationClientFactoryBase<HttpCommunicationClient>, IHttpCommunicationClientFactory
    {
        private static readonly Random _rand = new Random();
        private readonly ILogger _logger;

        private readonly HttpCommunicationOptions _options;

        public HttpCommunicationClientFactory(
            ILoggerFactory loggerFactory,
            ServicePartitionResolver resolver,
            IOptions<HttpCommunicationOptions> options)
            : base(resolver, options?.Value?.ExceptionHandlers, options?.Value?.DoNotRetryExceptionTypes)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _logger = loggerFactory.CreateLogger(HttpCommunicationDefaults.LoggerName);
            _options = options.Value;
        }

        protected override Task<HttpCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            // Create a communication client. This doesn't establish a session with the server.
            Uri endpointUri = CreateEndpointUri(endpoint);
            return Task.FromResult(new HttpCommunicationClient(endpointUri, _options.OperationTimeout));
        }

        protected override void AbortClient(HttpCommunicationClient client)
        {
            // Http communication doesn't maintain a communication channel, so nothing to abort.
        }

        protected override bool ValidateClient(HttpCommunicationClient clientChannel)
        {
            // Http communication doesn't maintain a communication channel, so nothing to validate.
            return true;
        }

        protected override bool ValidateClient(string endpoint, HttpCommunicationClient client)
        {
            Uri endpointUri = CreateEndpointUri(endpoint);
            bool equals = client.HttpClient.BaseAddress == endpointUri;
            return equals;
        }

        private Uri CreateEndpointUri(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint) || !endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The endpoint address is not valid. Please resolve again.");
            }

            if (!endpoint.EndsWith("/"))
            {
                endpoint = endpoint + "/";
            }

            // ServiceFabric publishes ASP.NET 5 projects with the endpoint "http://+:port" to listen on all IPs.
            // However, it's not possible to call the + url directly so we have to change it to localhost.
            endpoint = endpoint.Replace("+", "localhost");

            return new Uri(endpoint);
        }

        protected override bool OnHandleException(Exception ex, out ExceptionHandlingResult result)
        {
            // errors where we didn't get a response from the service.

            if (ex is TaskCanceledException || ex is TimeoutException)
            {
                _logger.LogWarning("Retrying Service call. Reason: {Reason}", ex.GetType().Name);
                return CreateExceptionHandlingRetryResult(false, ex, out result);
            }

            if (ex is ProtocolViolationException)
            {
                _logger.LogWarning("Retrying Service call. Reason: {Reason}, Details: {Details}", "ProtocolViolationException", ex);
                return CreateExceptionHandlingRetryResult(false, ex, out result);
            }

            var webEx = ex as WebException ?? ex.InnerException as WebException;
            if (webEx != null)
            {
                if (webEx.Status == WebExceptionStatus.Timeout ||
                    webEx.Status == WebExceptionStatus.RequestCanceled ||
                    webEx.Status == WebExceptionStatus.ConnectionClosed ||
                    webEx.Status == WebExceptionStatus.ConnectFailure)
                {
                    _logger.LogWarning("Retrying Service call. Reason: {Reason}, Details: {Details}", "WebExceptionStatus " + webEx.Status, ex);
                    return CreateExceptionHandlingRetryResult(false, webEx, out result);
                }
            }

            // we got a response from the service - let's try to get the StatusCode to see if we should retry.

            if (_options.RetryHttpStatusCodeErrors)
            {
                HttpStatusCode? httpStatusCode = null;
                HttpWebResponse webResponse = null;
                HttpResponseMessage responseMessage = null;

                var httpEx = ex as HttpResponseException;
                if (httpEx != null)
                {
                    responseMessage = httpEx.Response;
                    httpStatusCode = httpEx.Response.StatusCode;
                }
                else if (webEx != null)
                {
                    webResponse = webEx.Response as HttpWebResponse;
                    httpStatusCode = webResponse?.StatusCode;
                }

                if (httpStatusCode.HasValue)
                {
                    if (httpStatusCode == HttpStatusCode.NotFound)
                    {
                        // This could either mean we requested an endpoint that does not exist in the service API (a user error)
                        // or the address that was resolved by fabric client is stale (transient runtime error) in which we should re-resolve.

                        _logger.LogWarning("Retrying Service call. Reason: {Reason}", "HTTP 404");
                        result = new ExceptionHandlingRetryResult
                        {
                            IsTransient = false,
                            ExceptionId = "HTTP 404",
                            RetryDelay = TimeSpan.FromMilliseconds(100),
                            MaxRetryCount = 2
                        };
                        return true;
                    }

                    if ((int)httpStatusCode >= 500 && (int)httpStatusCode < 600)
                    {
                        // The address is correct, but the server processing failed.
                        // Retry the operation without re-resolving the address.

                        // we want to log the response in case it contains useful information (e.g. in dev environments)
                        string errorResponse = null;
                        if (webResponse != null)
                        {
                            using (StreamReader streamReader = new StreamReader(webResponse.GetResponseStream()))
                            {
                                errorResponse = streamReader.ReadToEnd();
                            }
                        }
                        else if (responseMessage != null)
                        {
                            errorResponse = responseMessage.Content.ReadAsStringAsync().Result;
                        }

                        _logger.LogWarning("Retrying Service call. Reason: {Reason}, Details: {Details}", "HTTP " + (int)httpStatusCode, errorResponse);
                        return CreateExceptionHandlingRetryResult(true, ex, out result);
                    }
                }
            }

            _logger.LogError($"Service call failed. ({ex.Message})", ex);
            return base.OnHandleException(ex, out result);
        }

        private bool CreateExceptionHandlingRetryResult(bool isTransient, Exception ex, out ExceptionHandlingResult result)
        {
            result = new ExceptionHandlingRetryResult()
            {
                IsTransient = isTransient,
                RetryDelay = TimeSpan.FromMilliseconds(_rand.NextDouble() * _options.MaxRetryBackoffInterval.TotalMilliseconds),
                ExceptionId = ex.GetType().Name,
                MaxRetryCount = _options.MaxRetryCount
            };
            return true;
        }
    }
}