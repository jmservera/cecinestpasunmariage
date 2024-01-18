using functions.TelegramBot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;

[assembly: RootNamespace("functions")]

bool isDevelopment = false;
var host = new HostBuilder()
.ConfigureServices((hostContext, services) =>
    {
        services.AddLocalization();

        services.AddTransient<Bot>();

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
    var factory = host.Services.GetRequiredService<ILoggerFactory>();
    var logger = factory.CreateLogger<Program>();
    logger.LogInformation("Running locally.");
    using var bot = host.Services.GetRequiredService<Bot>();
    await bot.RunBot();
    host.Run();
}
else
{
    host.Run();
}
