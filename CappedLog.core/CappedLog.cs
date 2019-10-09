using CappedLog.Internal;
using System;

namespace CappedLog
{
    public class CappedLog: ICappedLogStorrage
    {
        private struct Factory : IGetOrCreate<UInt64, CappedLogContainer>
        {
            private readonly CappedLogConf _config;

            public Factory(CappedLogConf config) { _config = config; }

            public UInt64 Key => _config.Key;

            public CappedLogContainer CreateValue() => new CappedLogContainer(_config);
        }

        internal InternalConcurrentDictionary<UInt64, CappedLogContainer> Containers { get; }

        public CappedLog()
        {
            Containers = new InternalConcurrentDictionary<ulong, CappedLogContainer>();
        }

        public virtual CappedLogContainer Create(CappedLogConf config) 
        {
            return Containers.Add(new Factory(config));
        }

        public virtual CappedLogContainer GetOrCreate(CappedLogConf config) 
        {
            return Containers.GetOrAdd(new Factory(config));
        }

        public void ForEach(Action<CappedLogContainer> action)
        {
            Containers.ForEach(action);
        }
    }
}
