using CappedLog;
using System;
using System.Threading;

namespace CappedLog
{
    public struct CappedLogMessage
    {
        public readonly DateTimeOffset Time;
        public readonly string Message;

        public CappedLogMessage(DateTimeOffset time, string message)
        {
            Time = time;
            Message = message;
        }

        public static CappedLogMessage Create(string message)
        {
            return message.ToLogMessage();
        }
        public static CappedLogMessage Create(string message, DateTimeOffset now)
        {
            return message.ToLogMessage(now);
        }

        public static implicit operator CappedLogMessage(string message) => message.ToLogMessage();
    }
}

public static class CappedLogMessageExtension
{
    private static int _prevTime = 0;

    public static CappedLogMessage ToLogMessage(this string message)
    {
        return message.ToLogMessage(DateTimeOffset.UtcNow);
    }

    public static CappedLogMessage ToLogMessage(this string message, DateTimeOffset now)
    {
        var diff = now.Ticks % TimeSpan.TicksPerMillisecond;
        var off = Interlocked.Increment(ref _prevTime) % TimeSpan.TicksPerMillisecond;
        return new CappedLogMessage(now.AddTicks(off - diff), message);
    }
}

