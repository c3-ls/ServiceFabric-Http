using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Diagnostics;
using System.Fabric;

namespace C3.ServiceFabric.AspNetCore.Hosting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (FabricRuntime fabricRuntime = FabricRuntime.Create())
                {
                    IApplicationEnvironment appEnv = PlatformServices.Default.Application;

                    IConfigurationRoot configurationRoot = new ConfigurationBuilder()
                        .AddJsonFile(WebHostDefaults.HostingJsonFile, true)
                        .AddEnvironmentVariables(prefix: WebHostDefaults.EnvironmentVariablesPrefix)
                        .AddCommandLine(args)
                        .Build();

                    string serviceTypeName = AspNetCoreService.GetServiceTypeName(configurationRoot, appEnv);

                    EventLog.WriteEntry("Application", string.Format("Registering ASP.NET service host for service {0}.", serviceTypeName), EventLogEntryType.Information);

                    fabricRuntime.RegisterStatelessServiceFactory(serviceTypeName, new AspNetCoreServiceFactory(configurationRoot));

                    EventLog.WriteEntry("Application", string.Format("Registered {0}", serviceTypeName), EventLogEntryType.Information);

                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.ToString(), EventLogEntryType.Error);

                throw;
            }
        }
    }
}