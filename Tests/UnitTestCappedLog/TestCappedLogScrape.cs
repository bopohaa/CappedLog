using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTestCapedLog
{
    [TestClass]
    public class TestCapedLogScrape
    {
        private class MyScrapeProcess : CappedLog.IScrapeProcess
        {
            public Task<int> Send(IReadOnlyList<CappedLog.CappedLogMetric> metrics, CancellationToken cancellation)
            {
                var result = 0;
                var temp = new List<CappedLog.CappedLogMessage>();
                foreach (var metric in metrics)
                {
                    if (metric.DequeueAll(temp) == 0)
                        continue;

                    //Do something

                    result += temp.Count;
                    temp.Clear();
                }

                return Task.FromResult(result);
            }
        }

        [TestMethod]
        public void TestSimpleScrape()
        {
            var log = new CappedLog.CappedLog();
            var conf = new CappedLog.CappedLogConfBuilder()
                .AddConstLabel("foo", "1")
                .SetDefaultCapacity(10)
                .Build();
            var container = log.GetOrCreate(conf);
            var scrape = CappedLog.CappedLogScrape.CreateScrape(log, new MyScrapeProcess());
            var metric = container.GetMetric(Array.Empty<string>());
            var cancellation = new CancellationTokenSource();

            //Sending messages to the log
            metric.TryEnqueue(() => CappedLog.CappedLogMessage.Create("msg1"));
            metric.TryEnqueue(() => CappedLog.CappedLogMessage.Create("msgN"));

            // Periodic collection of messages from the log
            var myScrapeProcessResult = scrape(cancellation.Token).Result;

            Assert.AreEqual(2, myScrapeProcessResult);
        }

        [TestMethod]
        public void TestManualScrape()
        {
            CappedLog.CappedLogScope scope;
            CappedLog.CappedLogConf conf2, conf3, conf1;
            CappedLog.CappedLogMetric metric11, metric12, metric2, metric3;
            Init(out scope, out conf2, out conf3, out conf1, out metric11, out metric12, out metric2, out metric3);

            int actualCnt1 = 0, actualCnt2 = 0, actualCnt3 = 0;
            Action<KeyValuePair<CappedLog.CappedLogConf, IReadOnlyCollection<CappedLog.CappedLogMessage>>> onScrape = e =>
            {
                var cnt = e.Value.Count;
                if (e.Key == conf1) actualCnt1 += cnt;
                else if (e.Key == conf2) actualCnt2 += cnt;
                else if (e.Key == conf3) actualCnt3 += cnt;
                else throw new NotImplementedException();
            };
            var scrapeInterval = TimeSpan.FromSeconds(1);
            var scrapeProcess = new ScrapeStub(onScrape);
            var cancel = new CancellationTokenSource();
            var complete = new SemaphoreSlim(0, 1);


            var start = DateTimeOffset.UtcNow;
            var scrapeTask = Scrape(CappedLog.CappedLogScrape.CreateScrape(scope, scrapeProcess), complete, scrapeInterval, cancel.Token);

            int cnt11 = 0, cnt12 = 0, cnt2 = 0, cnt3 = 0;
            Action insert = () =>
            {
                for (var i = 0; i < 60; ++i)
                {
                    if (Interlocked.Increment(ref cnt11) > 39 || !metric11.TryEnqueue($"message11 {i}"))
                        Interlocked.Decrement(ref cnt11);
                    if (Interlocked.Increment(ref cnt12) > 38 || !metric12.TryEnqueue($"message12 {i}"))
                        Interlocked.Decrement(ref cnt12);
                    if (Interlocked.Increment(ref cnt2) > 51 || !metric2.TryEnqueue($"message2 {i}"))
                        Interlocked.Decrement(ref cnt2);
                    if (Interlocked.Increment(ref cnt3) > 37 || !metric3.TryEnqueue($"message2 {i}"))
                        Interlocked.Decrement(ref cnt3);
                    Task.Delay(50).Wait();
                }
            };
            var tasks = new List<Task>();
            for (var i = 0; i < 4; ++i)
                tasks.Add(Task.Factory.StartNew(insert));

            Task.WaitAll(tasks.ToArray());
            Task.Delay(1500).Wait();
            complete.Release();
            var (successCnt, errorCnt) = scrapeTask.Result;

            var elapsed = (DateTimeOffset.UtcNow - start) / scrapeInterval + 1;
            Assert.AreEqual(errorCnt, 0);
            Assert.IsTrue(successCnt > 2);

            Assert.AreEqual(cnt11 + cnt12, actualCnt1);
            Assert.AreEqual(cnt2, actualCnt2);
            Assert.AreEqual(cnt3, actualCnt3);
        }

        [TestMethod]
        public void TestScrape()
        {
            CappedLog.CappedLogScope scope;
            CappedLog.CappedLogConf conf2, conf3, conf1;
            CappedLog.CappedLogMetric metric11, metric12, metric2, metric3;
            Init(out scope, out conf2, out conf3, out conf1, out metric11, out metric12, out metric2, out metric3);

            int errorCnt = 0, successCnt = 0;
            int actualCnt1 = 0, actualCnt2 = 0, actualCnt3 = 0;
            Action<KeyValuePair<CappedLog.CappedLogConf, IReadOnlyCollection<CappedLog.CappedLogMessage>>> onScrape = e =>
            {
                var cnt = e.Value.Count;
                if (e.Key == conf1) actualCnt1 += cnt;
                else if (e.Key == conf2) actualCnt2 += cnt;
                else if (e.Key == conf3) actualCnt3 += cnt;
                else throw new NotImplementedException();
            };
            var scrapeProcess = new ScrapeStub(onScrape);
            var cancel = new CancellationTokenSource();
            var scrape = new CappedLog.CappedLogScrape();

            scrape.OnError += e => { ++errorCnt; };
            scrape.OnSuccess += s => { ++successCnt; };

            scrape
                .SetScrapeInterval(TimeSpan.FromSeconds(1))
                .SetScrape(scope, scrapeProcess);

            var scrapeTask = scrape.Start(cancel.Token);

            int cnt11 = 0, cnt12 = 0, cnt2 = 0, cnt3 = 0;
            Action insert = () =>
            {
                for (var i = 0; i < 60; ++i)
                {
                    if (Interlocked.Increment(ref cnt11) > 39 || !metric11.TryEnqueue($"message11 {i}"))
                        Interlocked.Decrement(ref cnt11);
                    if (Interlocked.Increment(ref cnt12) > 38 || !metric12.TryEnqueue($"message12 {i}"))
                        Interlocked.Decrement(ref cnt12);
                    if (Interlocked.Increment(ref cnt2) > 51 || !metric2.TryEnqueue($"message2 {i}"))
                        Interlocked.Decrement(ref cnt2);
                    if (Interlocked.Increment(ref cnt3) > 37 || !metric3.TryEnqueue($"message2 {i}"))
                        Interlocked.Decrement(ref cnt3);
                    Task.Delay(50).Wait();
                }
            };
            var tasks = new List<Task>();
            for (var i = 0; i < 4; ++i)
                tasks.Add(Task.Factory.StartNew(insert));

            Task.WaitAll(tasks.ToArray());
            Task.Delay(1500).Wait();
            cancel.Cancel();

            Assert.AreEqual(errorCnt, 0);
            Assert.IsTrue(successCnt > 2);

            Assert.AreEqual(cnt11 + cnt12, actualCnt1);
            Assert.AreEqual(cnt2, actualCnt2);
            Assert.AreEqual(cnt3, actualCnt3);
        }

        private static void Init(out CappedLog.CappedLogScope scrape, out CappedLog.CappedLogConf conf2, out CappedLog.CappedLogConf conf3, out CappedLog.CappedLogConf conf1, out CappedLog.CappedLogMetric metric11, out CappedLog.CappedLogMetric metric12, out CappedLog.CappedLogMetric metric2, out CappedLog.CappedLogMetric metric3)
        {
            var log = new CappedLog.CappedLog();
            scrape = new CappedLog.CappedLogScope(log);
            var builder1 = new CappedLog.CappedLogConfBuilder()
                .AddConstLabel("one", "1")
                .AddConstLabels(new[] { "two", "2", "three", "3" })
                .SetDefaultCapacity(10);
            conf2 = builder1.Clone()
                .SetDefaultCapacity(20)
                .AddLabelNames(new[] { "four", "five" })
                .Build();
            conf3 = builder1
                .AddLabelName("four")
                .Build();
            conf1 = builder1
                .AddLabelName("six")
                .AddConstLabel("seven", "7")
                .Build();
            var conf0 = builder1
                .AddLabelName("nine")
                .Build();
            var container0 = log.GetOrCreate(conf0);
            var container1 = scrape.GetOrCreate(conf1);
            var container2 = scrape.GetOrCreate(conf2);
            var container3 = scrape.GetOrCreate(conf3);
            try { scrape.GetOrCreate(conf0); Assert.Fail(); }
            catch { }
            Assert.AreNotEqual(container1, container2);
            Assert.AreNotEqual(container1, container3);
            Assert.AreNotEqual(container2, container3);
            Assert.AreNotEqual(container1, container0);
            Assert.AreNotEqual(container2, container0);
            Assert.AreNotEqual(container3, container0);

            metric11 = container1.GetMetric(new[] { "41", "61" });
            metric12 = container1.GetMetric(new[] { "42", "62" });
            metric2 = container2.GetMetric(new[] { "4", "5" });
            metric3 = container3.GetMetric(new[] { "4" });
        }

        private async Task<(int, int)> Scrape(Func<CancellationToken, Task<int>> process, System.Threading.SemaphoreSlim complete, TimeSpan interval, CancellationToken cancellation)
        {
            var success = 0;
            var error = 0;
            var delay = interval;

            while (!cancellation.IsCancellationRequested)
            {
                if (await complete.WaitAsync(delay, cancellation))
                {
                    complete.Release();
                    break;
                }
                try
                {
                    var start = DateTimeOffset.UtcNow;
                    await process(cancellation);
                    var elapsed = DateTimeOffset.UtcNow - start;
                    delay = elapsed > interval ? TimeSpan.Zero : (interval - elapsed);
                    ++success;
                }
                catch { ++error; }
            }

            return (success, error);
        }

        private class ScrapeStub : CappedLog.IScrapeProcess
        {
            private readonly Action<KeyValuePair<CappedLog.CappedLogConf, IReadOnlyCollection<CappedLog.CappedLogMessage>>> _onScrape;

            public ScrapeStub(Action<KeyValuePair<CappedLog.CappedLogConf, IReadOnlyCollection<CappedLog.CappedLogMessage>>> on_scrape)
            {
                _onScrape = on_scrape;
            }

            public Task<int> Send(IReadOnlyList<CappedLog.CappedLogMetric> metrics, CancellationToken cancellation)
            {
                foreach (var metric in metrics)
                {
                    var temp = new List<CappedLog.CappedLogMessage>();
                    if (metric.DequeueAll(temp) == 0)
                        continue;
                    _onScrape(new KeyValuePair<CappedLog.CappedLogConf, IReadOnlyCollection<CappedLog.CappedLogMessage>>(metric.Config, temp));
                }

                return Task.FromResult(1);
            }
        }


    }

}
