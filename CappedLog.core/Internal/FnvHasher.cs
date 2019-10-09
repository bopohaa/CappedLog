using System;
using System.Collections.Generic;
using System.Text;

namespace CappedLog.Internal
{
    internal struct FnvHasher
    {
        private UInt64 _key;

        public FnvHasher(UInt64 key = 0)
        {
            _key = key > 0 ? key : 0xcbf29ce484222325;
        }

        public UInt64 Finish() => _key;

        public void Write(IReadOnlyList<string> values)
        {
            var size = values.Count;
            for (var i = 0; i < size; ++i)
                Write(values[i]);
        }

        public void Write(IReadOnlyList<KeyValuePair<string, string>> values)
        {
            var size = values.Count;
            for (var i = 0; i < size; ++i)
            {
                Write(values[i].Key);
                Write(values[i].Value);
            }
        }


        public void Write(string s)
        {
            unsafe
            {
                fixed (char* buffer = s)
                {
                    Write((byte*)buffer, s.Length * sizeof(char));
                }
            }
        }

        public unsafe void Write(byte* buffer, int length)
        {
            while (length > 0)
            {
                _key ^= *buffer;
                _key *= 0x100000001b3;
                --length;
                ++buffer;
            }
        }

    }
}
