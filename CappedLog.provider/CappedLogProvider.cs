using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CappedLog
{
    [ProviderAlias("CappedLog")]
    public class CappedLogProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly IOptionsMonitor<CappedLogLoggerOptions> _options;

        private readonly ConcurrentDictionary<string, CappedLogLogger> _loggers;
        private IDisposable _optionsReloadToken;
        private IExternalScopeProvider _scopeProvider;

        public CappedLogProvider(IOptionsMonitor<CappedLogLoggerOptions> options)
        {
            _loggers = new ConcurrentDictionary<string, CappedLogLogger>();
            _options = options;
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        public ILogger CreateLogger(string category_name)
        {
            return _loggers.GetOrAdd(category_name, CreateLoggerImplementation);
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
            foreach (var logger in _loggers)
            {
                logger.Value.ScopeProvider = _scopeProvider;
            }
        }

        private void ReloadLoggerOptions(CappedLogLoggerOptions options)
        {
            if (options.DefaultBuilder == null)
                throw new ArgumentNullException(nameof(options.DefaultBuilder));
            if (options.Storrage == null)
                throw new ArgumentNullException(nameof(options.Storrage));

            foreach (var logger in _loggers.Values)
            {
                logger.Options = options;
            }
        }

        private CappedLogLogger CreateLoggerImplementation(string category_name)
        {
            return new CappedLogLogger(category_name, _options.CurrentValue) { ScopeProvider = _scopeProvider };
        }
    }
}
