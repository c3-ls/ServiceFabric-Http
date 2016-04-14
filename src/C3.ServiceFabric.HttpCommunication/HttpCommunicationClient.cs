using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Fabric;
using System.Net.Http;

namespace C3.ServiceFabric.HttpCommunication
{
    /// <summary>
    /// Communication client that wraps the logic for talking to the service.
    /// Created by communication client factory.
    /// </summary>
    public class HttpCommunicationClient : ICommunicationClient, IDisposable
    {
        public HttpClient HttpClient { get; }

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
            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                // the calling system must decide whether to follow redirects or not.
                // (otherwise it can't e.g. change the browser url)
                AllowAutoRedirect = false
            };

            HttpClient = new HttpClient(httpClientHandler)
            {
                BaseAddress = baseAddress,
                Timeout = operationTimeout
            };
        }

        public void Dispose()
        {
            HttpClient?.Dispose();
        }
    }
}