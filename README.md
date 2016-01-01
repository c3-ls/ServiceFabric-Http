# HTTP Gateway for Azure Service Fabric services
This project contains an ASP.NET 5 middleware which can be used 
as a public gateway to your internal HTTP based Service Fabric services.

## Introduction

[Azure Service Fabric](https://azure.microsoft.com/en-us/services/service-fabric/)
is a cluster platform for hosting service-oriented applications.
It contains a feature-rich orchestration platform which allows you to configure
on how many nodes your services should run. If a node goes down or if 
Service Fabric has to reconfigure the service placements, your services are 
moved to other nodes automatically. Due to this dynamic nature, the cluster 
also contains a "naming service" which gives you an actual address for a service. 

Calling this naming service is fine if your code runs inside the cluster
as well. If you are using the default communication stacks or WCF for your
services then you can even use the built-in classes from the SDK 
(e.g. `ServiceProxy`) which makes this process transparent.

However if you want to access your services from a computer which is 
not part of the cluster you usually communicate with the cluster through 
a load balancer. If you set up a Service Fabric cluster in Microsoft Azure
this is automatically configured for you.
Since the load balancer is a mechanism outside of the cluster, it is not aware
of the Service Fabric placement settings and can't know where to redirect 
a call if the target service is only placed on one or a few of the cluster nodes.

For this reason, services which should be accessible through a load balancer 
have to be placed on *every* node (`InstanceCount="-1"`) and if you have multiple
services which should be routed through the load balancer, each needs its 
own public port. Both solutions have disadvantages and are often
not desired.

To solve this problem you need an additional service which is placed on every
node and acts as a gateway to your actual services. A gateway service has 
many advantages:

* You only need to setup one port on your load balancer (e.g 80 or 443 if you use HTTP)
* You can restrict access to your cluster resources on the network level
* The callers don't need to know anything about the cluster
* The gateway can translate protocols (your internal services may use different
communication protocols like WCF or the built-in TCP protocol)
* You can implement cross-cutting concerns like logging, security at the entry 
point of your cluster.
* ...

## What this project does

This project contains a library for resolving and forwarding requests to
services which use HTTP as their protocol. It has the following features:

### Resolving HTTP based services
The Service Fabric SDK contains classes for resolving services which use 
one of the built-in communication channels. (`ServiceProxy`, `ActorProxy`, 
`WcfCommunicationClientFactory`). 
However if your services use HTTP you have to manually resolve the endpoint
by using the lower-level classes like `ServicePartitionResolver`.

This project contains an HTTP implementation of `ICommunicationClientFactory` 
for *resolving HTTP-based services*. (see `HttpCommunication*.cs` for details)

### Transparent retry logic
The classes `ServiceProxy` and `ActorProxy` from the SDK implement 
transparent retry logic in case of failures (e.g. because a node went down
or the service returned a timeout). If you are using HTTP, you are on your own again.

This project implements the *retry functionality of `ICommunicationClientFactory`
for HTTP*. (see `HttpCommunicationClientFactory.cs` for details)

### Forwarding requests to the service
After the target service address was resolved, the incoming request is forwarded
to the target service by *copying all request headers and the request body*.
(see `HttpServiceGatewayMiddleware` for details)

### Support for multiple services
Since this project is implemented as an ASP.NET 5 middleware, you can use
the `IApplicationBuilder.Map()` feature to bind services to different paths
of your gateway. You can e.g. map "/service1" to "InternalService1" and 
"/service2" to "InternalService2". (see samples/HttpGateway/Startup.cs for details)

You can also use this feature to integrate the middleware into an existing application.

### Proxy Headers
Since your original client now no longer talks directly to the target service,
the target service doesn't get the original IP address of the client and it also 
doesn't know about the original URL which was requested by the client. 

For this reason, the gateway *adds standard proxy headers* to pass this information
to the target services. It implements the new 
["Forwarded" HTTP header](https://tools.ietf.org/html/rfc7239) and the non-standard 
headers `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto`.

It also sets a custom header called `X-Forwarded-PathBase` which contains the 
segment of the path under which the gateway hosts the service (e.g. "/service1").

This way, services can adjust their absolute URLs accordingly. 
(see `HttpRequestMessageExtensions` for details)

## Usage

If you want to create a new ASP.NET 5 gateway service, please take a look at the
project `samples/HttpGateway` for a basic example.

To use the middleware in your application you have to add it to your request
pipeline in your `Startup` class. The following using statement is required:

```
using C3.ServiceFabric.HttpServiceGateway;
```

If you want to redirect all requests to one service you can setup the middleware
to listen to the root path by invoking it like this in your `Startup.Configure` method:

```csharp
appBuilder.RunHttpServiceGateway(new HttpServiceGatewayOptions
{
    ServiceName = new Uri("fabric:/GatewaySample/HttpServiceService")
});
```

In most cases however, you would want to serve multiple services in your gateway.
Even if you only have one service it is still advisable to serve it on a subfolder
to be safe for the future.
To configure the middleware for a certain path, you have to invoke it like this
in your `Startup.Configure` method. You have to do this for each service.

```csharp
app.Map("/service", appBuilder =>
{
    appBuilder.RunHttpServiceGateway(new HttpServiceGatewayOptions
    {
        ServiceName = new Uri("fabric:/GatewaySample/HttpServiceService")
    });
});

// if you have a second service...
app.Map("/service2", appBuilder =>
{
    appBuilder.RunHttpServiceGateway(new HttpServiceGatewayOptions
    {
        ServiceName = new Uri("fabric:/GatewaySample/SomeOtherService")
    });
});
```

## Options

There are 2 different ways to configure this module:

There is a class called `GlobalConfig` which contains some default parameters. 
If you are not happy with these, you can change them at your application 
startup.

When you create the middleware for one service, you can pass an instance of 
`HttpServiceGatewayOptions` which allows you to adjust the retry behavior and
also to set a service partition key resolver if you use partitioned services.

## Known Issues and Considerations

The retry logic currently also retries the service call if it received a response
with a status code 5xx (Server Error). If your service is actually broken
or too busy, this gateway keeps retrying until the configured maximum is reached.
It does *not* yet implement a Circuit Breaker pattern.

Since the gateway retries failed requests, you have to make sure your services
are idempotent or do not persist any state in case they fail. There are multiple
scenarios where retries are problematic and can lead to logic beeing executed
multiple times:

* The gateway cancels requests after a specified timeout and retries. 
(Your service should react to this cancellation and abort)
* The response from your service might not reach the gateway due to network 
issues which also leads to a retry

Please take a detailed look at the implementation of the retry-logic to see if
it fits your needs!

## Contributions

Feel free to post issues, questions and feedback as an issue in this repository. 
If you want to contribute code please make sure you discuss the change with us before
you send the pull request if it contains major or breaking changes.