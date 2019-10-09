using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CappedLog
{
    class LokiScrapeWorker : BackgroundService
    {
        private readonly ILogger<LokiScrapeWorker> _logger;
        private readonly IDisposable _changeLoggerOptionsListener;
        private readonly IDisposable _changeLokiOptionsListener;
        private IScrapeProcess _process;
        private ICappedLogStorrage _storrage;
        private readonly CappedLogScrape _scrape;

        public LokiScrapeWorker(IOptionsMonitor<LokiConfig> loki_options, IOptionsMonitor<CappedLogLoggerOptions> logger_options, ILogger<LokiScrapeWorker> logger)
        {
            _logger = logger;
            _changeLoggerOptionsListener = logger_options.OnChange(OnChange_LoggerOptions);
            _changeLokiOptionsListener = loki_options.OnChange(OnChange_LokiOptions);
            _storrage = logger_options.CurrentValue.Storrage;
            _process = new LokiScrapeProcess(loki_options.CurrentValue.Url, loki_options.CurrentValue.Timeout);
            _scrape = new CappedLogScrape().SetScrapeInterval(loki_options.CurrentValue.ScrapeInterval).SetScrape(_storrage, _process);
            _scrape.OnError += Scrape_OnError;
            _scrape.OnSuccess += Scrape_OnSuccess;
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return _scrape.Start(stoppingToken);
        }

        public override void Dispose()
        {
            _changeLoggerOptionsListener.Dispose();
            _changeLokiOptionsListener.Dispose();
            base.Dispose();
        }

        private void OnChange_LoggerOptions(CappedLogLoggerOptions options)
        {
            _storrage = options.Storrage;
            _scrape.SetScrape(_storrage, _process);
        }

        private void OnChange_LokiOptions(LokiConfig options)
        {
            _process = new LokiScrapeProcess(options.Url, options.Timeout);
            _scrape.SetScrapeInterval(options.ScrapeInterval);
            _scrape.SetScrape(_storrage, _process);
        }

        private void Scrape_OnSuccess(int size)
        {
            _logger.LogInformation(new EventId(1, "ScrapeSuccess"), string.Empty);
        }

        private void Scrape_OnError(Exception ex)
        {
            _logger.LogError(new EventId(2, "ScrapeError"), ex, "{ex} ", ex);
        }
    }
}
