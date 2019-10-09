using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using Xunit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CappedLog;

namespace UnitTestCappedLogLoki
{
    public class TestScrapeLoki
    {
        [Fact]
        public void FromLogger()
        {
            var log = new CappedLog.CappedLog();
            var scope = new CappedLog.CappedLogScope(log);
            var builder = new CappedLog.CappedLogConfBuilder()
                .AddConstLabel("app", "UnitTestCappedLogLoki")
                .SetDefaultCapacity(10);
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, builder, scope);

            var cancel = new CancellationTokenSource();
            var scrapeProcess = new CappedLog.LokiScrapeProcess(new Uri("http://localhost:3100/api/prom/push"), TimeSpan.FromSeconds(10));
            var scrape = new CappedLog.CappedLogScrape();
            int errorCnt = 0, successCnt = 0;
            scrape
                .SetScrapeInterval(TimeSpan.FromSeconds(1))
                .SetScrape(scope, scrapeProcess);
            scrape.OnError += e => ++errorCnt;
            scrape.OnSuccess += s => ++successCnt;
            var scrapeTask = scrape.Start(cancel.Token);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger<TestScrapeLoki>>();
            logger.LogError(new IndexOutOfRangeException("error message 1"), "Error {}", "1");
            logger.LogCritical(new EventId(1, "Code 1"), new DllNotFoundException("error message 1"), "Critical {}", "2");
            logger.LogWarning(new EventId(2), "Warning {}", "3");

            Task.Delay(3000).Wait();
            cancel.Cancel();

            Assert.True(errorCnt == 0);
            Assert.True(successCnt > 0);
        }

        private static void ConfigureServices(IServiceCollection services, CappedLog.CappedLogConfBuilder builder, CappedLog.ICappedLogStorrage storrage)
        {
            services.AddLogging(configure => configure.AddCappedLog(conf=> {
                conf.DefaultBuilder = builder;
                conf.Storrage = storrage;
                conf.IncludeScopes = true;
            }));
        }
    }
}
