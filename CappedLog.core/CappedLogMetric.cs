using System;
using System.Collections.Generic;
using CappedLog.Internal;
using System.Linq;

namespace CappedLog
{
    /// <summary>
    /// Capped log Metric as log messages container with unique labels index
    /// </summary>
    /// <remarks>Thread safety: All public methods is thread safe</remarks>
    public class CappedLogMetric
    {
        private readonly IReadOnlyList<string> _labels;
        private CappedConcurrentQueue<CappedLogMessage> _messages;
        private CappedLogConf _config;

        public CappedLogMetric(CappedLogConf config, IEnumerable<string> labels)
        {
            _config = config;
            _labels = labels?.ToArray() ?? Array.Empty<string>();
            _messages = new CappedConcurrentQueue<CappedLogMessage>(_config.DefaultCapacity);
        }

        public int Capacity { get => _messages.Capacity; set => _messages.Capacity = value; }

        public IReadOnlyList<string> Labels => _labels;
        public CappedLogConf Config => _config;

        public bool TryEnqueue(CappedLogMessage message)
        {
            return _messages.TryEnqueue(message);
        }

        public bool TryEnqueue(Func<CappedLogMessage> message)
        {
            return _messages.TryEnqueue(message);
        }

        public int DequeueAll(IList<CappedLogMessage> messages)
        {
            return _messages.DequeueAll(ref messages);
        }

        public int DequeueAll<Tp, Tr>(IList<Tr> messages, Tp param, Func<Tp, CappedLogMessage, Tr> converter)
        {
            return _messages.DequeueAll(ref messages, param, converter);
        }
    }
}
