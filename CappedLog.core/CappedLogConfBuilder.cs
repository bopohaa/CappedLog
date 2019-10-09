using System;
using System.Collections.Generic;
using System.Linq;

namespace CappedLog
{
    public class CappedLogConfBuilder : ICloneable
    {
        public const int DEFAULT_CAPACITY = 1024;

        private List<KeyValuePair<string, string>> _constLabels;
        private List<string> _labelNames;
        private int _defaultCapacity;

        public CappedLogConfBuilder()
        {
            _constLabels = new List<KeyValuePair<string, string>>();
            _labelNames = new List<string>();
            _defaultCapacity = DEFAULT_CAPACITY;
        }

        public CappedLogConfBuilder SetDefaultCapacity(int capacity)
        {
            _defaultCapacity = capacity;
            return this;
        }

        public CappedLogConfBuilder AddLabelName(string name)
        {
            if (_labelNames.Contains(name))
                throw new ArgumentException("The specified key is already in the label collection", "name");
            _labelNames.Add(name);
            return this;
        }

        public CappedLogConfBuilder AddLabelNames(ICollection<string> names)
        {
            foreach (var name in names)
                AddLabelName(name);
            return this;
        }

        public CappedLogConfBuilder AddConstLabel(string name, string value)
        {
            if (_constLabels.Exists(e => e.Key == name))
                throw new ArgumentException("The specified key is already in the label collection", "name");
            _constLabels.Add(new KeyValuePair<string, string>(name, value));
            return this;
        }

        public CappedLogConfBuilder SetConstLabel(string name, string value)
        {
            var idx = _constLabels.FindIndex(e => e.Key == name);
            if (idx >= 0)
                _constLabels[idx] = new KeyValuePair<string, string>(name, value);
            else
                _constLabels.Add(new KeyValuePair<string, string>(name, value));

            return this;
        }

        public CappedLogConfBuilder AddConstLabels(IEnumerable<string> pairs)
        {
            foreach (var pair in pairs.ToKeyValuePairs())
                AddConstLabel(pair.Key, pair.Value);
            return this;
        }

        public CappedLogConf Build()
        {
            return new CappedLogConf(_constLabels, _labelNames, _defaultCapacity);
        }

        object ICloneable.Clone()
        {
            return new CappedLogConfBuilder()
            {
                _constLabels = _constLabels.ToList(),
                _defaultCapacity = _defaultCapacity,
                _labelNames = _labelNames.ToList(),
            };
        }

        public CappedLogConfBuilder Clone()
        {
            return (CappedLogConfBuilder)((ICloneable)this).Clone();
        }
    }
}
