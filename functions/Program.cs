using System.Diagnostics;
using functions.TelegramBot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
bool isDevelopment = false;
var host = new HostBuilder().ConfigureServices((hostContext, services) =>
    {
        isDevelopment = hostContext.HostingEnvironment.IsDevelopment();
        if (isDevelopment)
        {
            services.AddLogging(configure => configure.AddConsole());
        }
    })
    .ConfigureFunctionsWorkerDefaults()
    .Build();



if (isDevelopment)
{
    var factory = (ILoggerFactory)host.Services.GetRequiredService(typeof(ILoggerFactory));
    var logger = factory.CreateLogger<Program>();
    logger.LogInformation("Running locally.");
    using var bot = new Bot(factory);
    await bot.RunBot();
    host.Run();
}
else
{
    host.Run();
}
