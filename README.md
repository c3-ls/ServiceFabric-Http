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

Calling this naming service is fine if your code is inside the cluster
as well. If you are using the default communication stacks or WCF for your
services then you can even use the built-in classes from the SDK 
(e.g. ServiceProxy) which makes this process transparent.

However if you want to access your services from a computer which is 
not part of the cluster you usually communicate with the cluster through 
a load balancer. If you set up a Service Fabric cluster in Microsoft Azure
this is automatically configured for you.
Since the load balancer is a mechanism outside of the cluster, it is not aware
of the Service Fabric placement settings and can't know where to redirect 
a call if the target service is only placed on one or a few of the cluster nodes.

For this reason, services which should be accessible through a load balancer 
have to be placed on *every* node (InstanceCount="-1") and if you have multiple
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
communication protocols like Wcf or the built-in TCP protocol)
* You can implement cross-cutting concerns like logging, security at the entry 
point of your cluster.
* ...

## What this project does

This project contains a library for resolving and forwarding requests to
services which use HTTP as their protocol. It has the following features:

### Resolving HTTP based services
The Service Fabric SDK contains classes for resolving services which use 
one of the built-in communication channels. (ServiceProxy, ActorProxy, 
WcfCommunicationClientFactory). 
However if your services use HTTP you have to manually resolve the endpoint
by using the lower-level classes ServicePartitionResolver.

This project contains an HTTP implementation of ICommunicationClientFactory 
for *resolving HTTP-based services*.

### Transparent retry logic
The built-in classes ServiceProxy and ActorProxy from the SDK implement 
transparent retry logic in case of failures (e.g. because a node went down
or the service returned a timeout) If you are using HTTP, you are on your own again.

This project implements the *retry functionality of ICommunicationClientFactory
for HTTP*.

### Forwarding requests 1:1 to the service
After the target service was resolved, the incoming request is forwarded
to the target service by *copying all request headers and the request body*.

### Support for multiple services
Since this project is implemented as an ASP.NET 5 middleware, you can use
the IApplicationBuilder.Map() feature to bind services to different paths
of your gateway. You can e.g. map "/service1" to "InternalService1" and 
"/service2" to "InternalService2".

### Proxy Headers
Since your target service now no longer talks directly to the original client,
it doesn't get the original IP address of the client and it also doesn't know
about the original URL which was requested by the client. 

For this reason, the gateway *adds standard proxy headers* to pass this information
to the target services. It implements the new 
["Forwarded" HTTP header](https://tools.ietf.org/html/rfc7239)
and the non-standard headers X-Forwarded-For, X-Forwarded-Host, X-Forwarded-Proto.

It also sets a custom header called "X-Forwarded-PathBase" which contains the 
segment of the path under which the gateway hosts the service (e.g. "/service1").

This way, services can adjust their absolute URLs accordingly.