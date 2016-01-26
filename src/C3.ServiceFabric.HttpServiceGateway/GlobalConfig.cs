using System;

namespace C3.ServiceFabric.HttpServiceGateway
{
    /// <summary>
    /// This class provides a way to override some default settings.
    /// </summary>
    public static class GlobalConfig
    {
        public static string LoggerName = "C3.ServiceFabric.HttpServiceGateway";

        public static TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(12);

        public static Type[] DefaultDoNotRetryExceptionTypes = null;

        public static bool DefaultRetryHttpStatusCodeErrors = true;

        public static int DefaultMaxRetryCount = 9;

        public static TimeSpan DefaultMaxRetryBackoffInterval = TimeSpan.FromSeconds(3);
    }
}