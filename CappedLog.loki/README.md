# Scrape capped log to Grafana.Loki
Implementation of sending log messages to Grafana.Loki server

# Example of usage
```C#
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddCappedLog());

var cancel = new CancellationTokenSource();
var scrapeProcess = new CappedLog.LokiScrapeProcess(new Uri("http://localhost:3100/api/prom/push"), TimeSpan.FromSeconds(10));
var scrape = new CappedLog.CappedLogScrape();
int errorCnt = 0, successCnt = 0;
scrape
    .SetScrapeInterval(TimeSpan.FromSeconds(1))
    .SetScrape(CappedLogLoggerExtensions.DefaultCappedLog.Value, scrapeProcess);
scrape.OnError += e => ++errorCnt;
scrape.OnSuccess += s => ++successCnt;
var scrapeTask = scrape.Start(cancel.Token);
var serviceProvider = services.BuildServiceProvider();

var logger = serviceProvider.GetService<ILogger<TestScrapeLoki>>();
logger.LogError(new IndexOutOfRangeException("error message 1"), "Error {}", "1");
logger.LogCritical(new EventId(1, "Code 1"), new DllNotFoundException("error message 1"), "Critical {}", "2");
logger.LogWarning(new EventId(2), "Warning {}", "3");

Task.Delay(3000).Wait();
cancel.Cancel();

Assert.True(errorCnt == 0);
Assert.True(successCnt > 0);
```
