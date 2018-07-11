﻿using C3.ServiceFabric.HttpCommunication;
using Microsoft.AspNetCore.Http;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpDirect
{
    /// <summary>
    /// This could be a middleware, an ASP.NET Controller, some service, ...
    /// Just inject the IHttpCommunicationClientFactory
    /// </summary>
    public class DirectCommunicationMiddleware
    {
        private readonly IHttpCommunicationClientFactory _clientFactory;

        public DirectCommunicationMiddleware(RequestDelegate next,
            IHttpCommunicationClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            // create the Service Fabric client (it will resolve the address)
            var client = new ServicePartitionClient<HttpCommunicationClient>(
                _clientFactory,
                new Uri("fabric:/GatewaySample/HttpService"));

            // call your service.
            await client.InvokeWithRetryAsync(async x =>
            {
                HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, "api/values");

                HttpResponseMessage response = await x.SendAsync(req, context.RequestAborted, false);

                await context.Response.WriteAsync(DateTime.Now + " - Result from API: ");
                await context.Response.WriteAsync("Status: " + response.StatusCode + "; Body: ");
                await response.Content.CopyToAsync(context.Response.Body);
            }, context.RequestAborted);
        }
    }
}