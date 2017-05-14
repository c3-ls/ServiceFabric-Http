using System;

namespace C3.ServiceFabric.HttpCommunication
{
    /// <summary>
    /// Defaults for the HttpCommunication package. Can be overwritten at application startup.
    /// </summary>
    public class HttpCommunicationDefaults
    {
        /// <summary>
        /// The name of the logger.
        /// </summary>
        public static string LoggerName = "C3.ServiceFabric.HttpCommunication";

        /// <summary>
        /// The maximum time to wait for one single service request. (this value is reset for every retry)
        /// </summary>
        public static TimeSpan OperationTimeout = TimeSpan.FromSeconds(12);

        /// <summary>
        /// Defines whether certain status codes from the response should be retried. (eg. 500)
        /// </summary>
        public static bool RetryHttpStatusCodeErrors = false;

        /// <summary>
        /// The number of times a service request is retried in case of an error.
        /// </summary>
        public static int MaxRetryCount = 9;

        /// <summary>
        /// The maximum time to wait between two retries.
        /// </summary>
        public static TimeSpan MaxRetryBackoffInterval = TimeSpan.FromSeconds(3);
    }
}