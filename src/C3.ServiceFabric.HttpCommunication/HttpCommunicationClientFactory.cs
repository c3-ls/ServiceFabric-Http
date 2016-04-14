using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace C3.ServiceFabric.HttpCommunication
{
    /// <summary>
    /// Factory that creates clients that know to communicate with the service.
    /// Contains a service partition resolver that resolves a partition key
    /// and sets BaseAddress to the address of the replica that should serve a request.
    /// </summary>
    public class HttpCommunicationClientFactory : CommunicationClientFactoryBase<HttpCommunicationClient>, IHttpCommunicationClientFactory
    {
        private readonly HttpCommunicationOptions _options;

        public HttpCommunicationClientFactory(
            ServicePartitionResolver resolver,
            IEnumerable<IExceptionHandler> exceptionHandlers,
            IOptions<HttpCommunicationOptions> options)
            : base(resolver, exceptionHandlers)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

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
            client?.Dispose();
        }

        protected override bool ValidateClient(HttpCommunicationClient client)
        {
            // Http communication doesn't maintain a communication channel, so nothing to validate.
            return client != null;
        }

        protected override bool ValidateClient(string endpoint, HttpCommunicationClient client)
        {
            Uri endpointUri = CreateEndpointUri(endpoint);
            bool equals = client != null && client.HttpClient.BaseAddress == endpointUri;
            return equals;
        }

        private Uri CreateEndpointUri(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint) || !endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"The endpoint address '{endpoint}' is not a valid http endpoint!");
            }

            // BaseAddress must end with a trailing slash - this is critical for the usage of HttpClient!
            // http://stackoverflow.com/questions/23438416/why-is-httpclient-baseaddress-not-working
            if (!endpoint.EndsWith("/"))
            {
                endpoint = endpoint + "/";
            }

            // ServiceFabric publishes ASP.NET 5 projects with the endpoint "http://+:port" to listen on all IPs.
            // However, it's not possible to call the + url directly so we have to change it to localhost.
            endpoint = endpoint.Replace("+", "localhost");

            return new Uri(endpoint, UriKind.Absolute);
        }
    }
}