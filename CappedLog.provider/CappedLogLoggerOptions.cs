using Microsoft.Extensions.Logging;

namespace CappedLog
{
    public class CappedLogLoggerOptions
    {
        public bool IncludeScopes { get; set; }

        public string ApplicationName { get; set; }

        public int DefaultCapacity { get; set; }

        public LogLevel LogLevel { get; set; }

        public ICappedLogStorrage Storrage { get; set; }

        public CappedLogConfBuilder DefaultBuilder { get; set; }

    }
}
