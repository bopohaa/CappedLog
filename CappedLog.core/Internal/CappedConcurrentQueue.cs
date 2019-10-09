using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CappedLog.Internal
{
    internal class CappedConcurrentQueue<T>
    {
        private ConcurrentQueue<T> _messages;
        private int _capacity;
        private int _size;

        public int Capacity { get => Volatile.Read(ref _capacity); set => Volatile.Write(ref _capacity, value); }

        public CappedConcurrentQueue(int capacity)
        {
            _messages = new ConcurrentQueue<T>();
            _capacity = capacity;
            _size = 0;
        }

        public bool TryEnqueue(T message)
        {
            var size = Interlocked.Increment(ref _size);
            if (size > Volatile.Read(ref _capacity))
            {
                Interlocked.Decrement(ref _size);
                return false;
            }

            _messages.Enqueue(message);
            return true;
        }

        public bool TryEnqueue(Func<T> factory)
        {
            var size = Interlocked.Increment(ref _size);
            if (size > Volatile.Read(ref _capacity))
            {
                Interlocked.Decrement(ref _size);
                return false;
            }

            _messages.Enqueue(factory());
            return true;
        }


        public int DequeueAll(ref IList<T> messages)
        {
            int size = Volatile.Read(ref _capacity);
            int count = 0;
            while (count < size && _messages.TryDequeue(out var message))
            {
                messages.Add(message);
                ++count;
                Interlocked.Decrement(ref _size);
            }
            return count;
        }

        public int DequeueAll<Tp, Tr>(ref IList<Tr> messages, Tp param, Func<Tp, T, Tr> converter)
        {
            int size = Volatile.Read(ref _capacity);
            int count = 0;
            while (count < size && _messages.TryDequeue(out var message))
            {
                messages.Add(converter(param, message));
                ++count;
                Interlocked.Decrement(ref _size);
            }
            return count;
        }

    }
}
