using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace C3.ServiceFabric.HttpCommunication
{
    /// <summary>
    /// Provides default exception- &amp; retry-handling for the HTTP communication.
    /// </summary>
    public class HttpCommunicationExceptionHandler : IExceptionHandler
    {
        private static readonly Random _rand = new Random();

        private readonly HttpCommunicationOptions _options;
        private readonly ILogger _logger;

        public HttpCommunicationExceptionHandler(
            ILoggerFactory loggerFactory,
            IOptions<HttpCommunicationOptions> options)
        {
            _logger = loggerFactory.CreateLogger(HttpCommunicationDefaults.LoggerName);
            _options = options.Value;
        }

        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            if (exceptionInformation == null)
            {
                throw new ArgumentNullException(nameof(exceptionInformation));
            }

            var ex = exceptionInformation.Exception;

            // errors where we didn't get a response from the service.

            if (ex is TaskCanceledException || ex is TimeoutException)
            {
                _logger.RetryingServiceCall(ex.GetType().Name);

                return CreateExceptionHandlingRetryResult(false, ex, out result);
            }

            if (ex is ProtocolViolationException)
            {
                _logger.RetryingServiceCall("ProtocolViolationException", null, ex);

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
                    _logger.RetryingServiceCall("WebExceptionStatus " + webEx.Status, null, ex);

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

                        _logger.RetryingServiceCall("HTTP 404");

                        result = new ExceptionHandlingRetryResult(
                            exceptionId: "HTTP 404",
                            isTransient: false,
                            retryDelay: TimeSpan.FromMilliseconds(100),
                            maxRetryCount: 2);

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
                            // not sure if just calling ReadAsStringAsync().Result can result in a deadlock.
                            // so better safe than sorry...
                            // http://stackoverflow.com/questions/22628087/calling-async-method-synchronously
                            // AsyncEx library would be good but I don't want to take a dependency on that just for this one case.
                            errorResponse = Task.Run(() => responseMessage.Content.ReadAsStringAsync()).Result;
                        }

                        _logger.RetryingServiceCall($"HTTP {(int) httpStatusCode}", errorResponse);

                        return CreateExceptionHandlingRetryResult(true, ex, out result);
                    }
                }
            }

            _logger.ServiceCallFailed(ex);

            result = null;
            return false;
        }

        private bool CreateExceptionHandlingRetryResult(bool isTransient, Exception ex, out ExceptionHandlingResult result)
        {
            result = new ExceptionHandlingRetryResult(
                exceptionId: ex.GetType().Name,
                isTransient: isTransient,
                retryDelay: TimeSpan.FromMilliseconds(_rand.NextDouble()*_options.MaxRetryBackoffInterval.TotalMilliseconds),
                maxRetryCount: _options.MaxRetryCount);

            return true;
        }
    }
}