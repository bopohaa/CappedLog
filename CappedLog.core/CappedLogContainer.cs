using CappedLog.Internal;
using System;

namespace CappedLog
{
    /// <summary>
    /// Capped log Metric container.
    /// </summary>
    /// <remarks>Thread safety: All public methods is thread safe</remarks>
    public class CappedLogContainer
    {
        private struct Factory : IGetOrCreate<UInt64, CappedLogMetric>
        {
            public readonly string[] Labels;
            public readonly CappedLogConf Config;

            public Factory(string[] labels, CappedLogConf config)
            {
                Labels = labels;
                Config = config;
            }

            public ulong Key {
                get
                {
                    var hasher = new FnvHasher();
                    hasher.Write(Labels);
                    return hasher.Finish();
                }
            }

            public CappedLogMetric CreateValue() => new CappedLogMetric(Config, Labels);
        }

        private CappedLogConf _config;
        private InternalConcurrentDictionary<UInt64, CappedLogMetric> _metrics;

        public CappedLogContainer(CappedLogConf config)
        {
            _config = config;
            _metrics = new InternalConcurrentDictionary<ulong, CappedLogMetric>();
        }

        public CappedLogMetric GetMetric(string[] labels)
        {
            return _metrics.GetOrAdd(new Factory(labels, _config));
        }

        public void ForEach(Action<CappedLogMetric> action)
        {
            _metrics.ForEach(action);
        }
    }
}
