# HTTP Gateway for Azure Service Fabric services

This project contains two main components:

##### C3.ServiceFabric.HttpCommunication
An implementation of `ICommunicationClient` (part of the Service Fabric SDK) for HTTP. It resolves services and contains the retry logic. Please look at [Http Communication](https://github.com/c3-ls/ServiceFabric-HttpServiceGateway/wiki/Http-Communication) for details.

##### C3.ServiceFabric.HttpServiceGateway
The actual gateway, implemented as an ASP.NET Core middleware. Please look at [Http Gateway](https://github.com/c3-ls/ServiceFabric-HttpServiceGateway/wiki/Http-Gateway) for details.

You can find more documentation in the [Wiki](https://github.com/c3-ls/ServiceFabric-HttpServiceGateway/wiki) 

[![Build status](https://ci.appveyor.com/api/projects/status/glormo3hm3wsdwm4/branch/master?svg=true)](https://ci.appveyor.com/project/cwe1ss/servicefabric-httpservicegateway/branch/master)

## Contributions

Feel free to post issues, questions and feedback as an issue in this repository. 
If you want to contribute code please make sure you discuss the change with us before
you send the pull request if it contains major or breaking changes.
