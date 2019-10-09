using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CappedLog
{
    public class LokiScrapeProcess : IScrapeProcess
    {
        private readonly Uri _lokiUrl;

        private readonly HttpClient _client;
        private readonly List<CappedLogMessage> _temp;
        private readonly MemoryStream _buffer;

        public LokiScrapeProcess(Uri loki_url, TimeSpan? timeout)
        {
            _lokiUrl = loki_url;
            _client = new HttpClient();
            _temp = new List<CappedLogMessage>();
            _buffer = new MemoryStream();

            if (timeout.HasValue)
                _client.Timeout = timeout.Value;
        }

        public async Task<int> Send(IReadOnlyList<CappedLogMetric> metrics, CancellationToken cancellation)
        {
            var req = new Logproto.PushRequest();
            foreach (var metric in metrics)
            {
                _temp.Clear();
                if (metric.DequeueAll(_temp) == 0)
                    continue;

                var stream = new Logproto.Stream();
                foreach (var message in _temp)
                {
                    var entry = new Logproto.Entry()
                    {
                        Line = message.Message,
                        Ts = new Logproto.Timestamp()
                        {
                            Seconds = message.Time.ToUnixTimeSeconds(),
                            Nanos = (int)(message.Time.Ticks % TimeSpan.TicksPerMillisecond) * 100
                        }
                    };
                    stream.Entries.Add(entry);
                }
                stream.Labels = GetLabelsString(metric.Config, metric.Labels);
                req.Streams.Add(stream);
            }

            if (req.Streams.Count == 0)
                return 0;

            _buffer.SetLength(0);
            ProtoBuf.Serializer.Serialize(_buffer, req);
            _buffer.Position = 0;
            var data = _buffer.ToArray();
            var compressedData = new byte[SnappyLib.Snappy.MaxCompressedLength(data.Length)];
            var compressedSize = SnappyLib.Snappy.Compress(data, 0, data.Length, compressedData, 0, compressedData.Length);
            var content = new ByteArrayContent(compressedData, 0, compressedSize);
            using (var resp = await _client.PostAsync(_lokiUrl, content, cancellation))
                resp.EnsureSuccessStatusCode();

            return compressedSize;
        }

        private static string GetLabelsString(CappedLogConf config, IReadOnlyList<string> values)
        {
            var builder = new StringBuilder("{");
            builder.Append(string.Join(",", config.ConstLabels.Select(l => $"{l.Key}=\"{l.Value}\"").Concat(config.LabelNames.Zip(values, (n, v) => $"{n}=\"{v}\""))));
            builder.Append("}");
            return builder.ToString();
        }
    }
}
