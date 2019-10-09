using CappedLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ExampleConsole.Core21 { 
    class Program
    {
        static void Main(string[] args)
        {
            var log = new CappedLog.CappedLog();
            var builder = new CappedLog.CappedLogConfBuilder()
                .AddConstLabel("app", Assembly.GetAssembly(typeof(Program)).GetName().Name)
                .SetDefaultCapacity(10);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, builder, log);

            var cancel = new CancellationTokenSource();
            var scrapeProcess = new CappedLog.LokiScrapeProcess(new Uri("http://localhost:3100/api/prom/push"), TimeSpan.FromSeconds(10));
            var scrape = new CappedLog.CappedLogScrape();
            scrape
                .SetScrapeInterval(TimeSpan.FromSeconds(1))
                .SetScrape(log, scrapeProcess);
            var scrapeTask = scrape.Start(cancel.Token);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger<Program>>();
            logger.LogError(new IndexOutOfRangeException("error message 1"), "Error {}", "1");
            logger.LogCritical(new EventId(1, "Code 1"), new DllNotFoundException("error message 1"), "Critical {}", "2");
            logger.LogWarning(new EventId(2), "Warning {}", "3");

            Task.Delay(3000).Wait();
            cancel.Cancel();
        }

        private static void ConfigureServices(IServiceCollection services, CappedLog.CappedLogConfBuilder builder, CappedLog.ICappedLogStorrage storrage)
        {
            services.AddLogging(configure => configure.AddCappedLog(conf =>
            {
                conf.DefaultBuilder = builder;
                conf.Storrage = storrage;
                conf.IncludeScopes = true;
            }));
        }

    }

}
