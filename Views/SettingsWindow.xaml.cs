using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenWithTool.Services;
using OpenWithTool.ViewModels;

namespace OpenWithTool.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly IWindowingService _windowingService;
    private bool _initialized;

    public SettingsWindowViewModel ViewModel { get; }

    public SettingsWindow(SettingsWindowViewModel viewModel, IWindowingService windowingService)
    {
        ViewModel = viewModel;
        _windowingService = windowingService;

        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ViewModel.RequestClose += Close;
        Root.Loaded += Root_Loaded;
    }

    public static string RegistrationTitle(bool isRegistered) => isRegistered ? "Registered" : "Not registered";
    public static string RegistrationMessage(bool isRegistered) => isRegistered
        ? "OpenWith Tool is registered as a browser."
        : "OpenWith Tool is not registered as a browser.";
    public static InfoBarSeverity RegistrationSeverity(bool isRegistered) => isRegistered
        ? InfoBarSeverity.Success
        : InfoBarSeverity.Warning;

    public void ShowOwned(Window owner)
    {
        _windowingService.ConfigureDialog(this, owner, widthDip: 680, heightDip: 760, isResizable: false);
        Activate();
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        await ViewModel.InitializeAsync();
    }
}
