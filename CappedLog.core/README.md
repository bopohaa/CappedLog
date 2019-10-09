# Container of log
Implementation of a container for storing logs.

Each container has a maximum number of records upon reaching which writing to the container becomes impossible until the external reader processes all the records from the container.
This allows you to avoid memory overflow while increasing the flow of messages in the log between the operations of processing the log data (saving to disk or sending over the network).

Additionally, each container can have meta-information (a set of static and dynamic labels)

```C#
var log = new CappedLog.CappedLog();
var conf = new CappedLog.CappedLogConf(new[] { new KeyValuePair<string, string>("const_label_name", "const_label_value") }, new[] { "dynamic_label_name1", "dynamic_label_name2" }, 10);
var container = log.GetOrCreate(conf);
var metric = container.GetMetric(new[] { "dynamic_label_value1", "dynamic_label_value2" });
metric.TryEnqueue(() => CappedLog.CappedLogMessage.Create("msg1"));
metric.TryEnqueue(() => CappedLog.CappedLogMessage.Create("msgN"));
```

# Scrape log messages process
To collect accumulated messages in the log, you must implement a background message processing process that will be periodically started

```C#
class MyScrapeProcess : CappedLog.IScrapeProcess
{
    public Task<int> Send(IReadOnlyList<CappedLog.CappedLogMetric> metrics, CancellationToken cancellation)
    {
        var result = 0;
        var temp = new List<CappedLog.CappedLogMessage>();
        foreach (var metric in metrics)
        {
            if (metric.DequeueAll(temp) == 0)
                continue;

            //Do something

            result += temp.Count;
            temp.Clear();
        }

        return Task.FromResult(result);
    }
}

...

var log = new CappedLog.CappedLog();
var conf = new CappedLog.CappedLogConfBuilder()
    .AddConstLabel("foo", "1")
    .SetDefaultCapacity(10)
    .Build();
var container = log.GetOrCreate(conf);
var scrape = CappedLog.CappedLogScrape.CreateScrape(log, new MyScrapeProcess());
var metric = container.GetMetric(Array.Empty<string>());
var cancellation = new CancellationTokenSource();

//Sending messages to the log
metric.TryEnqueue(() => CappedLog.CappedLogMessage.Create("msg1"));
metric.TryEnqueue(() => CappedLog.CappedLogMessage.Create("msgN"));

// Periodic collection of messages from the log
var myScrapeProcessResult = scrape(cancellation.Token).Result;

Assert.AreEqual(2, myScrapeProcessResult);
```
