using functions.Messaging;
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
using functions.Audit;
using Microsoft.Extensions.DependencyInjection.Extensions;

[assembly: RootNamespace("functions")]

bool isDevelopment = false;
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((hostContext, services) =>
    {
        isDevelopment = hostContext.HostingEnvironment.IsDevelopment();
        if (isDevelopment)
        {
            services.AddLogging(configure => configure.AddConsole());
        }
        services.AddLocalization();
        services.AddTransient<Bot>();
        services.AddTransient<IStorageManager, StorageManager>();

        _ = services.AddTransient<IFaceClient, FaceClient>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            return new FaceClient(new ApiKeyServiceClientCredentials(
                                            config.GetValue<string>("VISION_KEY") ?? throw new InvalidOperationException("VISION_KEY is not set.")))
            {
                Endpoint = config.GetValue<string>("VISION_ENDPOINT") ?? throw new InvalidOperationException("VISION_ENDPOINT is not set.")
            };
        })
        .AddTransient<ImageAnalysisClient>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();

            return new ImageAnalysisClient(
                new Uri(config.GetValue<string>("COMPUTER_VISION_ENDPOINT") ?? throw new InvalidOperationException("COMPUTER_VISION_ENDPOINT is not set.")),
                new AzureKeyCredential(config.GetValue<string>("COMPUTER_VISION_KEY") ?? throw new InvalidOperationException("COMPUTER_VISION_KEY is not set.")));
        })
        .AddTransient<IEmailMessaging, EmailMessagingACS>();

        services.TryAdd(ServiceDescriptor.Singleton<IAuditServiceFactory, AuditInCosmosServiceFactory>());
        services.TryAdd(ServiceDescriptor.Singleton(typeof(IAuditService<>), typeof(AuditInCosmosService<>)));

        services.AddKernel();
        var config = hostContext.Configuration;

        services.AddAzureOpenAIChatCompletion(
            config.GetValue<string>("AOAI_DEPLOYMENT_NAME") ?? throw new InvalidOperationException("AOAI_DEPLOYMENT_NAME is not set."),
            config.GetValue<string>("AOAI_ENDPOINT") ?? throw new InvalidOperationException("AOAI_ENDPOINT is not set."),
            config.GetValue<string>("AOAI_KEY") ?? throw new InvalidOperationException("AOAI_KEY is not set.")
        );

        services.ConfigureHttpClientDefaults(c =>
        {
            c.ConfigureHttpClient((sp, cc) =>
            {
                cc.Timeout = TimeSpan.FromMinutes(4);
            });
        });

    })
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
