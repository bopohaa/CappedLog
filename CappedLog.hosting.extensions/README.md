# Implementation
Background worker to pushing Capped Log messages to Grafana.Loki server

# Usage
A minimal example of configure a simple console application (require `Microsoft.Extensions.Hosting`)
```C#
static void Main(string[] args)
{
    using (var host = new HostBuilder()
        .ConfigureLogging(c => c.AddCappedLog())
        .ConfigureServices(c => c.AddHostedService<ExampleWorker>()
            .AddCappedLogLokiScrape().Configure<CappedLog.LokiConfig>(o =>
                {
                    o.ScrapeInterval = TimeSpan.FromSeconds(1);
                    o.Timeout = TimeSpan.FromSeconds(10);
                    o.Url = new Uri("http://localhost:3100/api/prom/push");
                }))
        .Build())
    {
        host.Run();
    }
}

class ExampleWorker : BackgroundService
{
    private readonly ILogger<ExampleWorker> _logger;

    public ExampleWorker(ILogger<ExampleWorker> logger) { _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            _logger.LogError(new IndexOutOfRangeException("error message 1"), "Error {}", "1");
            _logger.LogCritical(new EventId(1, "Code 1"), new DllNotFoundException("error message 1"), "Critical {}", "2");
            _logger.LogWarning(new EventId(2), "Warning {}", "3");

            await Task.Delay(2000);
        }

    }
}
```
