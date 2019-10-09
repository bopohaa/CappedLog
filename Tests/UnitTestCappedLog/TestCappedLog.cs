using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace UnitTestCapedLog
{
    [TestClass]
    public class TestCapedLog
    {
        [TestMethod]
        public void TestCreateLogMessage()
        {
            {
                var msg1 = CappedLog.CappedLogMessage.Create("msg1");
                Assert.AreEqual(msg1.Message, "msg1");
                var msg2 = CappedLog.CappedLogMessage.Create("msg2");
                Assert.AreEqual(msg2.Message, "msg2");
                Assert.AreNotEqual(msg1.Time, msg2.Time);
            }
            {
                var now = DateTimeOffset.UtcNow;
                var msg1 = CappedLog.CappedLogMessage.Create("msg1", now);
                var msg2 = CappedLog.CappedLogMessage.Create("msg2", now);
                Assert.AreNotEqual(msg1.Time, msg2.Time);
            }
            {
                var now = DateTimeOffset.UtcNow;
                var msg1 = new CappedLog.CappedLogMessage(now, "msg1");
                var msg2 = new CappedLog.CappedLogMessage(now, "msg2");
                Assert.AreEqual(msg1.Time, msg2.Time);
            }
        }

        [TestMethod]
        public void TestCreateLogMetricConf()
        {
            {
                var log = new CappedLog.CappedLog();
                var constLabels1 = new[] { "one", "1" }.ToKeyValuePairs();
                var constLabels2 = new[] { "one", "2" }.ToKeyValuePairs();
                var constLabels3 = new[] { "one", "1", "two", "2" }.ToKeyValuePairs();
                var constLabels4 = new[] { "on1", "1" }.ToKeyValuePairs();
                var labelNames1 = new[] { "three" };
                var labelNames2 = new[] { "three", "four" };
                var defaultCapacity1 = 10;
                var defaultCapacity2 = 20;
                var conf1 = new CappedLog.CappedLogConf(constLabels1, labelNames1, defaultCapacity1);
                var conf2 = new CappedLog.CappedLogConf(constLabels2, labelNames1, defaultCapacity2);
                var conf3 = new CappedLog.CappedLogConf(constLabels3, labelNames1, defaultCapacity1);
                var conf4 = new CappedLog.CappedLogConf(constLabels4, labelNames1, defaultCapacity1);
                var conf5 = new CappedLog.CappedLogConf(constLabels1, labelNames2, defaultCapacity1);

                Assert.IsTrue(conf1.ConstLabels.SequenceEqual(constLabels1));
                Assert.IsTrue(conf2.ConstLabels.SequenceEqual(constLabels2));
                Assert.IsTrue(conf1.LabelNames.SequenceEqual(labelNames1));
                Assert.IsTrue(conf2.LabelNames.SequenceEqual(labelNames1));
                Assert.AreEqual(conf1.DefaultCapacity, defaultCapacity1);
                Assert.AreEqual(conf2.DefaultCapacity, defaultCapacity2);

                Assert.AreNotEqual(conf1.Key, conf2.Key);
                Assert.AreNotEqual(conf1.Key, conf3.Key);
                Assert.AreNotEqual(conf1.Key, conf4.Key);
                Assert.AreNotEqual(conf1.Key, conf5.Key);
            }
        }

        [TestMethod]
        public void TestCreateContainer()
        {
            var log = new CappedLog.CappedLog();
            var builder0 = new CappedLog.CappedLogConfBuilder()
                .SetDefaultCapacity(10)
                .SetConstLabel("one", "1");
            var builder1 = builder0.Clone()
                .AddConstLabels(new[] { "two", "2", "three", "33" })
                .SetConstLabel("three","3")
                ;
            var conf0 = builder0
                .AddConstLabels(new[] { "two", "20", "three", "30" })
                .Build();
            var conf1 = builder1
                .Build();
            var conf2 = builder1.Clone()
                .AddLabelNames(new[] { "four", "five" })
                .Build();
            var conf3 = builder1.Clone()
                .AddLabelName("four")
                .Build();
            var conf11 = builder1.Clone()
                .AddLabelName("four")
                .SetDefaultCapacity(20)
                .Build();

            var container0 = log.Create(conf0);
            var container1 = log.Create(conf1);
            Assert.AreNotEqual(container1, container0);

            var container2 = log.Create(conf2);
            Assert.AreNotEqual(container2, container1);
            Assert.AreNotEqual(container2, container0);

            var container3 = log.GetOrCreate(conf3);
            Assert.AreNotEqual(container3, container0);
            Assert.AreNotEqual(container3, container1);
            Assert.AreNotEqual(container3, container2);

            Assert.ThrowsException<ArgumentException>(() => log.Create(conf2));
            Assert.ThrowsException<ArgumentException>(() => log.Create(conf11));

            var container11 = log.GetOrCreate(conf11);
            Assert.AreEqual(container11, container3);
        }

        [TestMethod]
        public void TestCreateMetric()
        {
            var log = new CappedLog.CappedLog();
            var builder0 = new CappedLog.CappedLogConfBuilder()
                .SetDefaultCapacity(10)
                .AddConstLabel("one", "1");
            var builder1 = builder0.Clone()
                .AddConstLabels(new[] { "two", "2", "three", "3" });
            var conf0 = builder0
                .AddConstLabels(new[] { "two", "20", "three", "30" })
                .Build();
            var conf2 = builder1.Clone()
                .SetDefaultCapacity(20)
                .AddLabelNames(new[] { "four", "five" })
                .Build();
            var conf3 = builder1
                .AddLabelName("four")
                .Build();
            var conf1 = builder1
                .AddLabelName("six")
                .Build();
            var container0 = log.GetOrCreate(conf0);
            var container1 = log.GetOrCreate(conf1);
            var container2 = log.GetOrCreate(conf2);
            var container3 = log.GetOrCreate(conf3);
            Assert.AreNotEqual(container0, container1);
            Assert.AreNotEqual(container0, container2);
            Assert.AreNotEqual(container0, container3);
            Assert.AreNotEqual(container1, container2);
            Assert.AreNotEqual(container1, container3);
            Assert.AreNotEqual(container2, container3);

            var metric0 = container0.GetMetric(new[] { "41", "61" });
            var metric11 = container1.GetMetric(new[] { "41", "61" });
            var metric12 = container1.GetMetric(new[] { "42", "62" });
            var metric111 = container1.GetMetric(new[] { "41", "61" });
            Assert.AreNotEqual(metric0, metric11);
            Assert.AreNotEqual(metric0, metric12);
            Assert.AreNotEqual(metric11, metric12);
            Assert.AreEqual(metric11, metric111);

            var metric2 = container2.GetMetric(new[] { "4", "5" });
            var metric3 = container3.GetMetric(new[] { "4" });

            Assert.AreEqual(metric11.Capacity, 10);
            Assert.AreEqual(metric12.Capacity, 10);
            Assert.AreEqual(metric2.Capacity, 20);
            Assert.AreEqual(metric3.Capacity, 10);

            Assert.IsTrue(metric0.Config.ConstLabels.SequenceEqual(new[] { "one", "1", "two", "20", "three", "30" }.ToKeyValuePairs()));
            Assert.IsTrue(metric11.Config.ConstLabels.SequenceEqual(new[] { "one", "1", "two", "2", "three", "3" }.ToKeyValuePairs()));

            Assert.IsTrue(metric11.Config.LabelNames.SequenceEqual(new[] { "four", "six" }));
            Assert.IsTrue(metric11.Labels.SequenceEqual(new[] { "41", "61" }));
            Assert.IsTrue(metric12.Config.LabelNames.SequenceEqual(new[] { "four", "six" }));
            Assert.IsTrue(metric12.Labels.SequenceEqual(new[] { "42", "62" }));

            Assert.IsTrue(metric2.Config.LabelNames.SequenceEqual(new[] { "four", "five" }));
            Assert.IsTrue(metric2.Labels.SequenceEqual(new[] { "4", "5" }));

            Assert.IsTrue(metric3.Config.LabelNames.SequenceEqual(new[] { "four" }));
            Assert.IsTrue(metric3.Labels.SequenceEqual(new[] { "4" }));
        }

        [TestMethod]
        public void TestAddMessages()
        {
            var log = new CappedLog.CappedLog();
            var builder1 = new CappedLog.CappedLogConfBuilder()
                .AddConstLabel("one", "1")
                .AddConstLabels(new[] { "two", "2", "three", "3" })
                .SetDefaultCapacity(10);
            var conf2 = builder1.Clone()
                .SetDefaultCapacity(20)
                .AddLabelNames(new[] { "four", "five" })
                .Build();
            var conf3 = builder1
                .AddLabelName("four")
                .Build();
            var conf1 = builder1
                .AddLabelName("six")
                .AddConstLabel("seven", "7")
                .Build();
            var container1 = log.GetOrCreate(conf1);
            var container2 = log.GetOrCreate(conf2);
            var container3 = log.GetOrCreate(conf3);
            var metric11 = container1.GetMetric(new[] { "41", "61" });
            var metric12 = container1.GetMetric(new[] { "42", "62" });
            var metric2 = container2.GetMetric(new[] { "4", "5" });
            var metric3 = container3.GetMetric(new[] { "4" });

            var i11 = 0;
            var i12 = 0;
            var i2 = 0;
            var i3 = 0;
            Action insert = () =>
            {
                var id = Thread.CurrentThread.ManagedThreadId;
                for (var i = 0; i < 10; ++i)
                {
                    if (metric11.TryEnqueue(() => $"message11 {id}-{i}"))
                        Interlocked.Increment(ref i11);
                    if (metric12.TryEnqueue(() => $"message12 {id}-{i}"))
                        Interlocked.Increment(ref i12);
                    if (metric2.TryEnqueue(() => $"message2 {id}-{i}"))
                        Interlocked.Increment(ref i2);
                    if (metric3.TryEnqueue(() => $"message2 {id}-{i}"))
                        Interlocked.Increment(ref i3);
                    Task.Delay(1).Wait();
                }
            };
            var tasks = new List<Task>();
            for (var i = 0; i < 8; ++i)
                tasks.Add(Task.Factory.StartNew(insert));

            Task.WaitAll(tasks.ToArray());

            Assert.AreEqual(i11, 10);
            Assert.AreEqual(i12, 10);
            Assert.AreEqual(i2, 20);
            Assert.AreEqual(i3, 10);
        }
    }
}
