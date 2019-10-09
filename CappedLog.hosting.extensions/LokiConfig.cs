using System;
using System.Collections.Generic;
using System.Text;

namespace CappedLog
{
    public class LokiConfig
    {
        public Uri Url { get; set; }

        public TimeSpan ScrapeInterval { get; set; }

        public TimeSpan? Timeout { get; set; }
    }
}
