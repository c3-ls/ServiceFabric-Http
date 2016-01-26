using System;

namespace C3.ServiceFabric.HttpServiceGateway
{
    /// <summary>
    /// Configuration options for the retry behavior of the HttpClient.
    /// </summary>
    public class HttpCommunicationClientOptions
    {
        /// <summary>
        /// Exception types which should not result in a retry operation.
        /// </summary>
        public Type[] DoNotRetryExceptionTypes { get; set; } = GlobalConfig.DefaultDoNotRetryExceptionTypes;

        /// <summary>
        /// Defines whether certain status codes from the response should be retried. (eg. 500)
        /// </summary>
        public bool RetryHttpStatusCodeErrors { get; set; } = GlobalConfig.DefaultRetryHttpStatusCodeErrors;

        /// <summary>
        /// The number of times a service request is retried in case of an error.
        /// </summary>
        public int MaxRetryCount { get; set; } = GlobalConfig.DefaultMaxRetryCount;

        /// <summary>
        /// The maximum time to wait between two retries.
        /// </summary>
        public TimeSpan MaxRetryBackoffInterval { get; set; } = GlobalConfig.DefaultMaxRetryBackoffInterval;

        /// <summary>
        /// The maximum amount of time to wait for one single service request. (this value is reset for every retry)
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = GlobalConfig.DefaultOperationTimeout;
    }
}