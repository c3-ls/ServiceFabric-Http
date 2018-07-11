using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Fabric;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace C3.ServiceFabric.HttpCommunication
{
    /// <summary>
    /// Communication client that wraps the logic for talking to the service.
    /// Created by communication client factory.
    /// </summary>
    public class HttpCommunicationClient : ICommunicationClient, IDisposable
    {
        private HttpClient HttpClient { get; }

        private HttpClient HttpClientWithCookieForwarding { get; }

        /// <summary>
        /// The resolved service partition which contains the resolved service endpoints.
        /// </summary>
        public ResolvedServicePartition ResolvedServicePartition { get; set; }

        /// <summary>
        /// Gets or sets the name of the listener in the replica or instance to which the client is connected to.
        /// </summary>
        public string ListenerName { get; set; }

        /// <summary>
        /// Gets or sets the service endpoint to which the client is connected to.
        /// </summary>
        public ResolvedServiceEndpoint Endpoint { get; set; }

        public HttpCommunicationClient(Uri baseAddress, TimeSpan operationTimeout)
        {
            HttpClient = CreateClient(baseAddress, operationTimeout, false);
            HttpClientWithCookieForwarding = CreateClient(baseAddress, operationTimeout, true);
        }

        /// <summary>
        /// Send an HTTP request using the appropriate <see cref="HttpClient"/>, depending on <see cref="shouldForwardCookies"/>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="shouldForwardCookies"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken,
            bool shouldForwardCookies)
        {
            var client = shouldForwardCookies ? HttpClientWithCookieForwarding : HttpClient;
            return client.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Validates this <see cref="HttpCommunicationClient"/> against the given <see cref="baseAddress"/>
        /// </summary>
        /// <param name="baseAddress"></param>
        /// <returns></returns>
        public bool Validate(Uri baseAddress) =>
            HttpClient.BaseAddress == baseAddress &&
            HttpClientWithCookieForwarding.BaseAddress == baseAddress;

        public void Dispose()
        {
            HttpClient?.Dispose();
        }

        private HttpClient CreateClient(Uri baseAddress, TimeSpan operationTimeout, bool forwardsCookies)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                // the calling system must decide whether to follow redirects or not.
                // (otherwise it can't e.g. change the browser url)
                AllowAutoRedirect = false,
                UseCookies = !forwardsCookies
            };

            return new HttpClient(httpClientHandler)
            {
                BaseAddress = baseAddress,
                Timeout = operationTimeout
            };
        }
    }
}