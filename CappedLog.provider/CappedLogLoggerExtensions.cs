using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace CappedLog
{
    public static class CappedLogLoggerExtensions
    {
        public static Lazy<CappedLog> DefaultCappedLog = new Lazy<CappedLog>(true);

        public static ILoggingBuilder AddCappedLog(this ILoggingBuilder builder, Action<CappedLogLoggerOptions> configure = null)
        {
            builder.AddConfiguration();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, CappedLogProvider>());
            LoggerProviderOptions.RegisterProviderOptions<CappedLogLoggerOptions, CappedLogProvider>(builder.Services);

            builder.Services.Configure<CappedLogLoggerOptions>(opt => ConfigureCappedLog(opt, configure));

            return builder;
        }

        private static void ConfigureCappedLog(CappedLogLoggerOptions opt, Action<CappedLogLoggerOptions> configure)
        {
            var conf = new CappedLogConfBuilder()
                .AddConstLabel("app", opt.ApplicationName ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)
                .SetDefaultCapacity(opt.DefaultCapacity == 0 ? 10 : opt.DefaultCapacity);
            opt.DefaultBuilder = conf;
            configure?.Invoke(opt);
            if (opt.Storrage == null)
                opt.Storrage = DefaultCappedLog.Value;
        }
    }
}
