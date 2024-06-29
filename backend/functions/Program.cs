using functions.TelegramBot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using functions.Storage;
using Microsoft.Extensions.Azure;

[assembly: RootNamespace("functions")]

bool isDevelopment = false;
var host = new HostBuilder()
.ConfigureServices((hostContext, services) =>
    {
        services.AddLocalization();

        services.AddTransient<Bot>();
        services.AddTransient<IFileUploader, FileUploader>();

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
    // when running locally we need to instantiate the bot manually
    var factory = host.Services.GetRequiredService<ILoggerFactory>();
    var logger = factory.CreateLogger<Program>();
    logger.LogInformation("Running {Program} locally.", nameof(Program));
    Bot? bot = null;
    try
    {
        try
        {
            bot = host.Services.GetRequiredService<Bot>();
            await bot.RunBot();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running bot. Won't be available locally.");
        }
        host.Run();
    }
    finally
    {
        bot?.Dispose();
    }
}
else
{
    host.Run();
}
