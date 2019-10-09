using System;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CappedLog
{
    public class CappedLogLogger : ILogger
    {
        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();

            private NullScope() { }

            public void Dispose() { }
        }

        [ThreadStatic]
        private static StringBuilder _logBuilder;
        private CappedLogLoggerOptions _options;
        private CappedLogContainer[] _containersWithLevels;
        private CappedLogConfBuilder _builder;
        private ICappedLogStorrage _storrage;

        internal string CategoryName { get; }
        internal IExternalScopeProvider ScopeProvider { get; set; }
        internal CappedLogLoggerOptions Options
        {
            get => _options; set
            {
                _options = value;
                ReloadCappedLog(_options.DefaultBuilder, _options.Storrage);
            }
        }

        public CappedLogLogger(string category_name, CappedLogLoggerOptions options)
        {
            CategoryName = category_name ?? throw new ArgumentNullException(nameof(category_name));
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state) ?? NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= _options.LogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            var container = GetContainer(logLevel);
            var metric = container.GetMetric(new[] { eventId.ToString(), exception?.GetType().Name ?? string.Empty });

            metric.TryEnqueue(() =>
            {
                var logBuilder = _logBuilder;
                _logBuilder = null;

                if (logBuilder == null)
                    logBuilder = new StringBuilder();

                GetScopeInformation(logBuilder);
                logBuilder.Append(formatter(state, exception));
                var message = logBuilder.ToString();

                logBuilder.Clear();
                if (logBuilder.Capacity > 1024)
                    logBuilder.Capacity = 1024;
                _logBuilder = logBuilder;

                return message.ToLogMessage();
            });
        }

        private void ReloadCappedLog(CappedLogConfBuilder builder, ICappedLogStorrage storrage)
        {
            lock (this)
            {
                var levelEnumCount = Enum.GetValues(typeof(LogLevel)).Cast<int>().Max() + 1;
                _storrage = storrage;
                _builder = builder
                    .Clone()
                    .AddConstLabel("category", CategoryName)
                    .AddLabelNames(new[] { "code", "exception" });
                System.Threading.Volatile.Write(ref _containersWithLevels, new CappedLogContainer[levelEnumCount]);
            }
        }

        private CappedLogContainer GetContainer(LogLevel logLevel)
        {
            var idx = (int)logLevel;
            if (idx > (int)LogLevel.None)
                idx = (int)LogLevel.Error;

            var container = System.Threading.Volatile.Read(ref _containersWithLevels)[idx];
            if (container != null)
                return container;

            lock (this)
            {
                var conf = _builder.Clone().AddConstLabel("level", GetLogLevelString(logLevel)).Build();
                container = _storrage.GetOrCreate(conf);
                _containersWithLevels[idx] = container;
            }

            return container;
        }

        private void GetScopeInformation(StringBuilder stringBuilder)
        {
            var scopeProvider = ScopeProvider;
            if (Options.IncludeScopes && scopeProvider != null)
            {
                scopeProvider.ForEachScope((scope, state) =>
                {
                    var builder = state;
                    builder.Append(scope);
                }, stringBuilder);
            }
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trace";
                case LogLevel.Debug:
                    return "debug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "error";
                case LogLevel.Critical:
                    return "crit";
                case LogLevel.None:
                    return "none";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }
    }
}
