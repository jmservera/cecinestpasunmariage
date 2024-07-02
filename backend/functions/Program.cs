using functions.TelegramBot;
using Microsoft.Extensions.Configuration; // Add this using directive
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using functions.Storage;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.SemanticKernel;
using Azure.AI.Vision.ImageAnalysis;
using Azure;

[assembly: RootNamespace("functions")]

bool isDevelopment = false;
var host = new HostBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLocalization();

        services.AddTransient<Bot>();
        services.AddTransient<IStorageManager, StorageManager>();
        services.AddTransient<IFaceClient, FaceClient>(provider =>
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(
                                            Environment.GetEnvironmentVariable("VISION_KEY") ?? throw new InvalidOperationException("VISION_KEY is not set.")))
            {
                Endpoint = Environment.GetEnvironmentVariable("VISION_ENDPOINT") ?? throw new InvalidOperationException("VISION_ENDPOINT is not set.")
            };
        });

        services.AddTransient<ImageAnalysisClient>(provider=>{
            return new ImageAnalysisClient(
                new Uri(Environment.GetEnvironmentVariable("COMPUTER_VISION_ENDPOINT") ?? throw new InvalidOperationException("COMPUTER_VISION_ENDPOINT is not set.")),
                new AzureKeyCredential(Environment.GetEnvironmentVariable("COMPUTER_VISION_KEY") ?? throw new InvalidOperationException("COMPUTER_VISION_KEY is not set.")));
        });

        services.AddKernel();
        services.AddAzureOpenAIChatCompletion(
            Environment.GetEnvironmentVariable("AOAI_DEPLOYMENT_NAME") ?? throw new InvalidOperationException("AOAI_DEPLOYMENT_NAME is not set."),
            Environment.GetEnvironmentVariable("AOAI_ENDPOINT") ?? throw new InvalidOperationException("AOAI_ENDPOINT is not set."),
            Environment.GetEnvironmentVariable("AOAI_KEY") ?? throw new InvalidOperationException("AOAI_KEY is not set.")
        );

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
