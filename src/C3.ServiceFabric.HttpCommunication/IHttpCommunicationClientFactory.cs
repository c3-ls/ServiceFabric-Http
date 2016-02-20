using Microsoft.ServiceFabric.Services.Communication.Client;

namespace C3.ServiceFabric.HttpCommunication
{
    public interface IHttpCommunicationClientFactory : ICommunicationClientFactory<HttpCommunicationClient>
    {
    }
}