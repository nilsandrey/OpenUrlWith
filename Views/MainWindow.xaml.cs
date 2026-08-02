using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenWithTool.Models;
using OpenWithTool.Services;
using OpenWithTool.ViewModels;
using Windows.System;

namespace OpenWithTool.Views;

public sealed partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private SettingsWindow? _settingsWindow;
    private SitesSettingsWindow? _sitesSettingsWindow;

    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        IServiceProvider serviceProvider,
        IWindowingService windowingService)
    {
        ViewModel = viewModel;
        _serviceProvider = serviceProvider;

        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        windowingService.ConfigureMainWindow(this);

        ViewModel.RequestClose += Close;
        ViewModel.OpenSettingsRequested += OpenSettingsWindow;
        ViewModel.OpenSitesSettingsRequested += OpenSitesSettingsWindow;
        ViewModel.RequestListFocus += FocusBrowserList;
        Root.Loaded += (_, _) => FocusBrowserList();
    }

    public static Visibility BoolToVisibility(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public static Visibility InvertBoolToVisibility(bool value) => value ? Visibility.Collapsed : Visibility.Visible;
    public static BitmapImage? IconFromPath(string path) => string.IsNullOrWhiteSpace(path)
        ? null
        : new BitmapImage(new Uri(path));
    public static string BrowserProfileAutomationId(string browserName) => $"BrowserProfile_{browserName}";
    public static string FocusButtonAutomationId(string browserName) => $"FocusBrowser_{browserName}";

    private void FocusBrowserList()
    {
        if (ViewModel.IsBrowserFocused)
            FocusedProfileGrid.Focus(FocusState.Programmatic);
        else
            BrowserList.Focus(FocusState.Programmatic);
    }

    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        ViewModel.OnUserInteraction();
        if (e.Key == VirtualKey.Escape)
        {
            ViewModel.CancelCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void Root_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.OnUserInteraction();
    }

    private void BrowserList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel.LaunchCommand.CanExecute(null))
            ViewModel.LaunchCommand.Execute(null);
    }

    private void FocusBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BrowserInfo browser })
            ViewModel.FocusBrowserCommand.Execute(browser);
    }

    private void FocusedProfileGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FocusedProfileGrid.SelectedItem is BrowserProfile profile
            && !ReferenceEquals(profile, ViewModel.FocusedProfile))
        {
            ViewModel.SelectFocusedProfileCommand.Execute(profile);
        }
    }

    private void OpenSettingsWindow()
    {
        try
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }

            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            _settingsWindow = settingsWindow;
            settingsWindow.Closed += async (_, _) =>
            {
                _settingsWindow = null;
                await ViewModel.ReloadDisplaySettingsAsync();
                Activate();
            };
            settingsWindow.ShowOwned(this);
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync("Settings could not be opened", ex.Message);
        }
    }

    private void OpenSitesSettingsWindow()
    {
        try
        {
            if (_sitesSettingsWindow != null)
            {
                _sitesSettingsWindow.Activate();
                return;
            }

            var sitesSettingsWindow = _serviceProvider.GetRequiredService<SitesSettingsWindow>();
            _sitesSettingsWindow = sitesSettingsWindow;
            sitesSettingsWindow.Closed += (_, _) =>
            {
                _sitesSettingsWindow = null;
                Activate();
            };
            sitesSettingsWindow.ShowOwned(this);
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync("Remembered sites could not be opened", ex.Message);
        }
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = Root.XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "Close",
            DefaultButton = ContentDialogButton.Close
        };
        await dialog.ShowAsync();
    }
}
