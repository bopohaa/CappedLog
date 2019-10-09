using CappedLog.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CappedLog
{
    public class CappedLogConf
    {
        private UInt64 _key;

        public IReadOnlyList<KeyValuePair<string, string>> ConstLabels { get; }
        public IReadOnlyList<string> LabelNames { get; }
        public int DefaultCapacity { get; }
        public UInt64 Key => _key;

        public CappedLogConf(IEnumerable<KeyValuePair<string, string>> const_labels, IEnumerable<string> label_names, int default_capacity)
        {
            ConstLabels = const_labels.ToArray();
            LabelNames = label_names.ToArray();
            DefaultCapacity = default_capacity;

            var hasher = new FnvHasher();
            hasher.Write("\"");
            hasher.Write(ConstLabels);
            hasher.Write("\",\"");
            hasher.Write(LabelNames);
            hasher.Write("\"");

            _key = hasher.Finish();
        }
    }

    public readonly struct CappedLogContainerFactory
    {
        public ICappedLogStorrage Log { get; }
        public CappedLogConf Config { get; }

        public CappedLogContainerFactory(CappedLogConf conf, ICappedLogStorrage log)
        {
            Config = conf;
            Log = log;
        }

        public CappedLogContainer Create()
        {
            return Log.Create(Config);
        }

        public CappedLogContainer GetOrCreate()
        {
            return Log.GetOrCreate(Config);
        }
    }
}

public static class LogMetricConfExtensions
{
    public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(this IEnumerable<string> pairs)
    {
        using (var e = pairs.GetEnumerator())
            while (e.MoveNext())
            {
                var key = e.Current;
                if (!e.MoveNext())
                    throw new ArgumentException("Even number of values required");
                yield return new KeyValuePair<string, string>(key, e.Current);
            }
    }
}

