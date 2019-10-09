using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CappedLog.Internal
{
    internal class InternalConcurrentDictionary<Tk, Tv> : IDisposable
    {
        private SortedList<Tk, int> _indexes;
        private List<Tv> _values;
        private ReaderWriterLockSlim _lock;

        public InternalConcurrentDictionary()
        {
            _indexes = new SortedList<Tk, int>();
            _values = new List<Tv>();
            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        public void Dispose()
        {
            _lock.Dispose();
        }


        public Tv Add<Tf>(Tf factory) where Tf : IGetOrCreate<Tk, Tv>
        {
            _lock.EnterWriteLock();
            try
            {
                var key = factory.Key;
                _indexes.Add(key, _values.Count);

                var value = factory.CreateValue();
                _values.Add(value);

                return value;
            }
            finally { _lock.ExitWriteLock(); }
        }

        public Tv GetOrAdd<Tf>(Tf factory) where Tf : IGetOrCreate<Tk, Tv>
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                int idx;
                var key = factory.Key;
                if (_indexes.TryGetValue(key, out idx))
                    return _values[idx];
                _lock.EnterWriteLock();
                try
                {
                    if (_indexes.TryGetValue(key, out idx))
                        return _values[idx];

                    _indexes.Add(key, _values.Count);
                    var value = factory.CreateValue();
                    _values.Add(value);

                    return value;
                }
                finally { _lock.ExitWriteLock(); }
            }
            finally { _lock.ExitUpgradeableReadLock(); }
        }

        public void ForEach(Action<Tv> action)
        {
            _lock.EnterReadLock();
            try
            {
                _values.ForEach(action);
            }
            finally { _lock.ExitReadLock(); }
        }
    }
}
