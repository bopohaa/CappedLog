using Microsoft.Extensions.Configuration;
using System;
using CappedLog;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CapedLogHostingDiExtensions
    {
        private static CappedLog.CappedLog _capedLog = new CappedLog.CappedLog();

        public static IServiceCollection AddCappedLogLokiScrape(this IServiceCollection services)
        {
            return services.AddHostedService<LokiScrapeWorker>();
        }

        public static IServiceCollection AddCappedLogLokiScrape(this IServiceCollection services, IConfigurationSection loki_config)
        {
            if (loki_config == null)
                throw new ArgumentNullException(nameof(loki_config));

            return services
                .Configure<LokiConfig>(loki_config)
                .AddCappedLogLokiScrape();
        }
    }
}
