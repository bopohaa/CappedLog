using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CappedLog
{
    public interface ICappedLogStorrage
    {
        CappedLogContainer Create(CappedLogConf config);

        CappedLogContainer GetOrCreate(CappedLogConf config);

        void ForEach(Action<CappedLogContainer> action);
    }

    public interface IScrapeProcess
    {
        Task<int> Send(IReadOnlyList<CappedLogMetric> metrics, CancellationToken cancellation);
    }
}
