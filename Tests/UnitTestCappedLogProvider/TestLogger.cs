using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Xunit;

namespace UnitTestCappedLogProvider
{
    public class TestLogger
    {
        [Fact]
        public void LogMessages()
        {
            var categoryName = "Test category";
            var log = new CappedLog.CappedLog();
            var builder = new CappedLog.CappedLogConfBuilder();
            var options = new CappedLog.CappedLogLoggerOptions() { DefaultBuilder = builder, Storrage = log, LogLevel = LogLevel.Trace };
            var logger = new CappedLog.CappedLogLogger(categoryName, options);
            var data = new List<(CappedLog.CappedLogMetric, CappedLog.CappedLogMessage)>();

            logger.LogError(new IndexOutOfRangeException("error message 1"), "Error {}", "1");
            log.ForEach(c => c.ForEach(m => m.DequeueAll(data, m, (p, d) => (p, d))));
            Assert.Single(data);
            Assert.Equal(new[] { "category", categoryName, "level", "error" }.ToKeyValuePairs(), data[0].Item1.Config.ConstLabels);
            Assert.Equal(new[] { "code", "exception" }, data[0].Item1.Config.LabelNames);
            Assert.Equal(new[] { "0", typeof(IndexOutOfRangeException).Name }, data[0].Item1.Labels);
            Assert.Equal("Error 1", data[0].Item2.Message);

            logger.LogCritical(new EventId(1, "Code 1"), new DllNotFoundException("error message 1"), "Critical {}", "2");
            log.ForEach(c => c.ForEach(m => m.DequeueAll(data, m, (p, d) => (p, d))));
            Assert.Equal(2, data.Count);
            Assert.Equal(new[] { "category", categoryName, "level", "crit" }.ToKeyValuePairs(), data[1].Item1.Config.ConstLabels);
            Assert.Equal(new[] { "code", "exception" }, data[1].Item1.Config.LabelNames);
            Assert.Equal(new[] { "Code 1", typeof(DllNotFoundException).Name }, data[1].Item1.Labels);
            Assert.Equal("Critical 2", data[1].Item2.Message);

            logger.LogWarning(new EventId(2), "Warning {}", "3");
            log.ForEach(c => c.ForEach(m => m.DequeueAll(data, m, (p, d) => (p, d))));
            Assert.Equal(3, data.Count);
            Assert.Equal(new[] { "category", categoryName, "level", "warn" }.ToKeyValuePairs(), data[2].Item1.Config.ConstLabels);
            Assert.Equal(new[] { "code", "exception" }, data[2].Item1.Config.LabelNames);
            Assert.Equal(new[] { "2", string.Empty }, data[2].Item1.Labels);
            Assert.Equal("Warning 3", data[2].Item2.Message);

        }
    }
}
