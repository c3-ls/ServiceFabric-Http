using System;
using Microsoft.Extensions.Logging;

namespace C3.ServiceFabric.HttpCommunication
{
    /// <summary>
    /// Logger methods for this library.
    /// </summary>
    public static class HttpCommunicationLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _createClient;
        private static readonly Action<ILogger, string, string, Exception> _validateClient;
        private static readonly Action<ILogger, string, Exception> _abortClient;

        private static readonly Action<ILogger, string, string, Exception> _retryingServiceCall;
        private static readonly Action<ILogger, Exception> _serviceCallFailed;

        static HttpCommunicationLoggerExtensions()
        {
            _createClient = LoggerMessage.Define<string>(
                logLevel: LogLevel.Information,
                eventId: 10,
                formatString: "Creating client for {endpoint}");

            _validateClient = LoggerMessage.Define<string, string>(
                logLevel: LogLevel.Debug,
                eventId: 11,
                formatString: "Validating client {ClientEndpoint}/{PassedEndpoint}");

            _abortClient = LoggerMessage.Define<string>(
                logLevel: LogLevel.Information,
                eventId: 12,
                formatString: "Aborting client for {endpoint}");

            _retryingServiceCall = LoggerMessage.Define<string, string>(
                logLevel: LogLevel.Warning,
                eventId: 20,
                formatString: "Retrying service call ({Reason}), Details: {Details}");

            _serviceCallFailed = LoggerMessage.Define(
                logLevel: LogLevel.Warning,
                eventId: 21,
                formatString: "Service call failed");
        }

        public static void CreateClient(this ILogger logger, string endpoint)
        {
            _createClient(logger, endpoint, null);
        }

        public static void ValidateClient(this ILogger logger, HttpCommunicationClient client, string endpoint = null)
        {
            _validateClient(logger, client?.Endpoint?.Address, endpoint, null);
        }

        public static void AbortClient(this ILogger logger, HttpCommunicationClient client)
        {
            _abortClient(logger, client?.Endpoint?.Address, null);
        }

        public static void RetryingServiceCall(this ILogger logger, string reason, string details = null, Exception ex = null)
        {
            _retryingServiceCall(logger, reason, details, ex);
        }

        public static void ServiceCallFailed(this ILogger logger, Exception ex = null)
        {
            _serviceCallFailed(logger, ex);
        }
    }
}