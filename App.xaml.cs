using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OpenWithTool.Services;
using OpenWithTool.ViewModels;
using OpenWithTool.Views;

namespace OpenWithTool;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Handle command line arguments
        if (e.Args.Length > 0)
        {
            var arg = e.Args[0].ToLowerInvariant();
            
            if (arg == "--register" || arg == "--hide-icons" || arg == "--show-icons")
            {
                // These are browser registration commands, just exit
                Shutdown();
                return;
            }
        }

        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // Get the URL from command line arguments
        var url = e.Args.Length > 0 ? e.Args[0] : "https://www.example.com";

        // Get the ViewModel instance
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        
        // Create and show main window with the same ViewModel instance
        var mainWindow = new MainWindow(viewModel, _serviceProvider);
        
        MainWindow = mainWindow;
        
        // Initialize with URL and show
        mainWindow.Show();
        _ = viewModel.InitializeAsync(url);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IBrowserDetectionService, BrowserDetectionService>();
        services.AddSingleton<IBrowserLauncherService, BrowserLauncherService>();
        services.AddSingleton<IUrlProtocolRegistrationService, UrlProtocolRegistrationService>();

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<SettingsWindowViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
    }
}