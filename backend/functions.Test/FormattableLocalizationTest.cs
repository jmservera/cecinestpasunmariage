using functions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace functions.Test;
public class FormattableLocalizationTest
{
    readonly IStringLocalizer<TelegramBot> localizer;

    private readonly ITestOutputHelper _testOutputHelper;

    public FormattableLocalizationTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLocalization();
        serviceCollection.AddLogging(builder => builder.AddProvider(new XunitLoggerProvider(_testOutputHelper)));
        var serviceProvider = serviceCollection.BuildServiceProvider();
        localizer = serviceProvider.GetRequiredService<IStringLocalizer<TelegramBot>>();
    }

    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    public void TestUser(string lang)
    {
        ChangeThreadCulture(lang);
        Assert.Equal(lang, localizer.GetString("language").Value);
        var metaPromptForUser = localizer.GetString("MetaPromptUser");
        var metaPromptPartUser = string.Format(metaPromptForUser, "Ramon", "Ramon Ito");
        Assert.Contains("Ramon", metaPromptPartUser);
        Assert.Contains("Ramon Ito", metaPromptPartUser);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    public void TestMarkdown(string lang)
    {
        ChangeThreadCulture(lang);
        Assert.Equal(lang, localizer.GetString("language").Value);
        var metaPromptForMarkdown = localizer.GetString("MetaPromptMarkdown");
        var metaPromptMarkdown = string.Format(metaPromptForMarkdown, "a\nb\nc\nd");
        Assert.Contains("a\nb\nc\nd", metaPromptMarkdown);
    }

    private static void ChangeThreadCulture(string lang)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(lang);
        System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(lang);
    }
}