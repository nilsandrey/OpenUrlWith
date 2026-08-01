using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenWithTool.Models;
using OpenWithTool.Services;
using OpenWithTool.ViewModels;

namespace OpenWithTool.Views;

public sealed partial class SitesSettingsWindow : Window
{
    private readonly IWindowingService _windowingService;
    private bool _initialized;

    public SitesSettingsWindowViewModel ViewModel { get; }

    public SitesSettingsWindow(SitesSettingsWindowViewModel viewModel, IWindowingService windowingService)
    {
        ViewModel = viewModel;
        _windowingService = windowingService;

        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ViewModel.RequestClose += Close;
        Root.Loaded += Root_Loaded;
    }

    public static string MatchTypeName(SiteMatchType matchType) => matchType switch
    {
        SiteMatchType.ExactUrl => "Exact URL",
        SiteMatchType.Domain => "Entire domain",
        SiteMatchType.Path => "First path segment",
        _ => matchType.ToString()
    };

    public static InfoBarSeverity StatusSeverity(bool hasError) => hasError
        ? InfoBarSeverity.Error
        : InfoBarSeverity.Informational;

    public void ShowOwned(Window owner)
    {
        _windowingService.ConfigureDialog(this, owner, widthDip: 840, heightDip: 660, isResizable: true);
        Activate();
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;

        _initialized = true;
        await ViewModel.InitializeAsync();
    }

    private async void RemoveRule_Click(object sender, RoutedEventArgs e)
    {
        var rule = ViewModel.SelectedRule;
        if (rule == null)
            return;

        var dialog = new ContentDialog
        {
            XamlRoot = Root.XamlRoot,
            Title = "Remove remembered site?",
            Content = $"Remove the rule for {rule.Pattern}? Links matching it will no longer open automatically.",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            await ViewModel.RemoveSelectedRuleCommand.ExecuteAsync(null);
    }
}
