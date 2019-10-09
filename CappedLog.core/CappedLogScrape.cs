using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CappedLog
{
    using ScrapeFunc = Func<CancellationToken, Task<int>>;

    public class CappedLogScrape : IDisposable
    {
        private readonly ReaderWriterLockSlim _lock;
        private readonly LinkedList<ScrapeFunc> _scrapes;
        private long _scrapeInterval;

        public TimeSpan ScrapeInterval
        {
            get => TimeSpan.FromTicks(Volatile.Read(ref _scrapeInterval));
        }

        public event Action<Exception> OnError;
        public event Action<int> OnSuccess;

        public CappedLogScrape()
        {
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _scrapes = new LinkedList<Func<CancellationToken, Task<int>>>();
            _scrapeInterval = TimeSpan.TicksPerSecond;
        }

        public CappedLogScrape SetScrape(ICappedLogStorrage storrage, IScrapeProcess process)
        {
            _lock.EnterWriteLock();
            try
            {
                _scrapes.AddLast(CreateScrape(storrage, process));
            }
            finally { _lock.ExitWriteLock(); }
            return this;
        }
        public CappedLogScrape SetScrape(ScrapeFunc scrape)
        {
            _lock.EnterWriteLock();
            try
            {
                _scrapes.AddLast(scrape);
            }
            finally { _lock.ExitWriteLock(); }
            return this;
        }


        public CappedLogScrape SetScrapeInterval(TimeSpan interval)
        {
            Volatile.Write(ref _scrapeInterval, interval.Ticks);
            return this;
        }

        public async Task Start(CancellationToken cancellation)
        {
            var nodes = new List<LinkedListNode<Func<CancellationToken, Task<int>>>>();
            var tasks = new List<Task<int>>();

            while (true)
            {
                cancellation.ThrowIfCancellationRequested();
                nodes.Clear();
                tasks.Clear();
                _lock.EnterReadLock();
                try
                {
                    var node = _scrapes.First;
                    while (node != null)
                    {
                        nodes.Add(node);
                        node = node.Next;
                    }
                }
                finally { _lock.ExitReadLock(); }
                {
                    var count = nodes.Count;
                    for (var i = 0; i < count; ++i)
                    {
                        tasks.Add(nodes[i].Value(cancellation));
                    }
                }

                var start = DateTimeOffset.UtcNow;
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }
                var elapsed = DateTimeOffset.UtcNow - start;

                var size = 0;
                _lock.EnterWriteLock();
                try
                {
                    var count = tasks.Count;
                    for (var i = 0; i < count; ++i)
                    {
                        if (tasks[i].Status != TaskStatus.RanToCompletion || tasks[i].Result == 0)
                        {
                            if (nodes[i].Next != null)
                                _scrapes.Remove(nodes[i]);
                        }
                        else
                            size += tasks[i].Result;
                    }
                }
                finally { _lock.ExitWriteLock(); }
                if (size > 0)
                    OnSuccess?.Invoke(size);

                var delay = ScrapeInterval - elapsed;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, cancellation);
            }
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

        public static ScrapeFunc CreateScrape(ICappedLogStorrage storrage, IScrapeProcess process)
        {
            var metrics = new List<CappedLogMetric>();
            Action<CappedLogMetric> addMetric = m => metrics.Add(m);
            Action<CappedLogContainer> addMetrics = c => c.ForEach(addMetric);
            Func<Task<int>, int> clearMetrics = t => { metrics.Clear(); return t.Result; };

            return (CancellationToken cancellation) =>
            {
                storrage.ForEach(addMetrics);
                return process.Send(metrics, cancellation).ContinueWith(clearMetrics);
            };

        }

    }
}
