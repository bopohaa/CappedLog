# Features

* Capping logger (see info in [CappedLog.core](https://github.com/bopohaa/CappedLog/blob/master/CappedLog.core/README.md) )
* Supporting logging API
* Automatic log message labeles

# Usage
### Configure
```C#
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddCappedLog();
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```
### Create logger
```C#
public class AboutModel : PageModel
{
    private readonly ILogger _logger;

    public AboutModel(ILogger<AboutModel> logger)
    {
        _logger = logger;
    }
```
### Write message
```C#
public void OnGet()
{
    Message = $"About page visited at {DateTime.UtcNow.ToLongTimeString()}";
    _logger.LogInformation("Message displayed: {Message}", Message);
}
```
Adds log message with text "Message displayed: About page visited at 10:30:15 AM" and labels:
* app={current executable assembly name}
* category=AboutModel
* level=info
* code=0
* exception=
