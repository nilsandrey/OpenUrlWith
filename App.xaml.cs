using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using OpenWithTool.Services;
using OpenWithTool.ViewModels;
using OpenWithTool.Views;

namespace OpenWithTool;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public static Window? MainWindow { get; private set; }
    public static DispatcherQueue DispatcherQueue { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var launchArgument = GetLaunchArgument(args.Arguments);
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        var services = new ServiceCollection();
        ConfigureServices(services, DispatcherQueue);
        _serviceProvider = services.BuildServiceProvider();

        if (HandleRegistrationCommand(launchArgument))
        {
            ExitWithoutWindow();
            return;
        }

        var url = string.IsNullOrWhiteSpace(launchArgument)
            ? "https://www.example.com"
            : launchArgument;

        if (await TryLaunchRememberedSiteAsync(url))
        {
            ExitWithoutWindow();
            return;
        }

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Closed += (_, _) => DisposeServices();
        mainWindow.Activate();
        await mainWindow.ViewModel.InitializeAsync(url);
    }

    private async Task<bool> TryLaunchRememberedSiteAsync(string url)
    {
        if (_serviceProvider == null)
            return false;

        var configurationService = _serviceProvider.GetRequiredService<IConfigurationService>();
        var rememberedSiteService = _serviceProvider.GetRequiredService<IRememberedSiteService>();
        var settings = await configurationService.GetSettingsAsync();
        var matchingRule = rememberedSiteService.FindMatchingRule(settings.RememberedSiteRules, url);
        if (matchingRule == null)
            return false;

        var browserDetectionService = _serviceProvider.GetRequiredService<IBrowserDetectionService>();
        var browserLauncherService = _serviceProvider.GetRequiredService<IBrowserLauncherService>();
        var browsers = await browserDetectionService.GetAvailableBrowsersAsync();
        var browser = browsers.FirstOrDefault(candidate =>
            candidate.Name.Equals(matchingRule.BrowserName, StringComparison.OrdinalIgnoreCase));
        var profile = browser?.Profiles.FirstOrDefault(candidate =>
            candidate.Name.Equals(matchingRule.ProfileName, StringComparison.OrdinalIgnoreCase));

        if (browser == null || (!string.IsNullOrEmpty(matchingRule.ProfileName) && profile == null))
            return false;

        browser.SelectedProfile = profile ?? browser.SelectedProfile;
        return await browserLauncherService.LaunchBrowserAsync(browser, url);
    }

    private static string GetLaunchArgument(string activationArguments)
    {
        var commandLineArgument = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
        var argument = !string.IsNullOrWhiteSpace(commandLineArgument)
            ? commandLineArgument
            : activationArguments;

        return argument?.Trim().Trim('"') ?? string.Empty;
    }

    private bool HandleRegistrationCommand(string argument)
    {
        if (_serviceProvider == null)
            return false;

        var registrationService = _serviceProvider.GetRequiredService<IUrlProtocolRegistrationService>();
        if (argument.Equals("--register", StringComparison.OrdinalIgnoreCase))
        {
            Environment.ExitCode = registrationService.RegisterAsDefaultBrowser() ? 0 : 1;
            return true;
        }

        if (argument.Equals("--unregister", StringComparison.OrdinalIgnoreCase))
        {
            Environment.ExitCode = registrationService.UnregisterAsDefaultBrowser() ? 0 : 1;
            return true;
        }

        return argument.Equals("--hide-icons", StringComparison.OrdinalIgnoreCase)
               || argument.Equals("--show-icons", StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureServices(IServiceCollection services, DispatcherQueue dispatcherQueue)
    {
        services.AddSingleton(dispatcherQueue);
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IBrowserDetectionService, BrowserDetectionService>();
        services.AddSingleton<IBrowserLauncherService, BrowserLauncherService>();
        services.AddSingleton<IUrlProtocolRegistrationService, UrlProtocolRegistrationService>();
        services.AddSingleton<IRememberedSiteService, RememberedSiteService>();
        services.AddSingleton<IBrowserIconService, BrowserIconService>();
        services.AddSingleton<IWindowingService, WindowingService>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsWindowViewModel>();
        services.AddTransient<SitesSettingsWindowViewModel>();

        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<SitesSettingsWindow>();
    }

    private void DisposeServices()
    {
        MainWindow = null;
        _serviceProvider?.Dispose();
        _serviceProvider = null;
    }

    private void ExitWithoutWindow()
    {
        DisposeServices();
        Exit();
    }
}
