using System;

namespace CappedLog
{

    public class CappedLogScope : CappedLog
    {
        private readonly struct Factory : IGetOrCreate<UInt64, CappedLogContainer>
        {
            private readonly ICappedLogStorrage _storrage;
            private readonly CappedLogConf _config;

            public Factory(CappedLogConf config, ICappedLogStorrage storrage)
            {
                _storrage = storrage;
                _config = config;
            }

            public ulong Key => _config.Key;

            public CappedLogContainer CreateValue() => _storrage.Create(_config);
        }

        private readonly ICappedLogStorrage _parent;

        public CappedLogScope(ICappedLogStorrage parent_storrage)
        {
            _parent = parent_storrage;
        }

        public override CappedLogContainer Create(CappedLogConf config)
        {
            return Containers.Add(new Factory(config, _parent));
        }

        public override CappedLogContainer GetOrCreate(CappedLogConf config)
        {
            return Containers.GetOrAdd(new Factory(config, _parent));
        }
    }
}
