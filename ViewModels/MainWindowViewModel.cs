using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using OpenWithTool.Models;
using OpenWithTool.Services;

namespace OpenWithTool.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IBrowserDetectionService _browserDetectionService;
    private readonly IConfigurationService _configurationService;
    private readonly IBrowserLauncherService _browserLauncherService;
    private readonly IRememberedSiteService _rememberedSiteService;
    private readonly IBrowserIconService _browserIconService;
    private readonly DispatcherQueueTimer _autoSelectTimer;

    private BrowserInfo? _selectedBrowser;

    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimerMessage))]
    public partial int TimeRemaining { get; set; }

    [ObservableProperty]
    public partial bool IsTimerActive { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool RememberSelection { get; set; }

    [ObservableProperty]
    public partial SiteMatchOption? SelectedMatchOption { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBrowserFocused))]
    [NotifyPropertyChangedFor(nameof(FocusedBrowserDisplayName))]
    [NotifyPropertyChangedFor(nameof(FocusedBrowserIconPath))]
    [NotifyPropertyChangedFor(nameof(FocusedProfiles))]
    [NotifyPropertyChangedFor(nameof(FocusedProfile))]
    public partial BrowserInfo? FocusedBrowser { get; set; }

    [ObservableProperty]
    public partial bool ShowSettingsButton { get; set; } = true;

    public MainWindowViewModel(
        IBrowserDetectionService browserDetectionService,
        IConfigurationService configurationService,
        IBrowserLauncherService browserLauncherService,
        IRememberedSiteService rememberedSiteService,
        IBrowserIconService browserIconService,
        DispatcherQueue dispatcherQueue)
    {
        _browserDetectionService = browserDetectionService;
        _configurationService = configurationService;
        _browserLauncherService = browserLauncherService;
        _rememberedSiteService = rememberedSiteService;
        _browserIconService = browserIconService;

        Browsers = new ObservableCollection<BrowserInfo>();
        MatchOptions = new ObservableCollection<SiteMatchOption>();

        _autoSelectTimer = dispatcherQueue.CreateTimer();
        _autoSelectTimer.Interval = TimeSpan.FromSeconds(1);
        _autoSelectTimer.Tick += AutoSelectTimer_Tick;
    }

    public ObservableCollection<BrowserInfo> Browsers { get; }
    public ObservableCollection<SiteMatchOption> MatchOptions { get; }
    public bool IsBrowserFocused => FocusedBrowser != null;
    public string FocusedBrowserDisplayName => FocusedBrowser?.DisplayName ?? string.Empty;
    public string FocusedBrowserIconPath => FocusedBrowser?.IconCachePath ?? string.Empty;
    public IReadOnlyList<BrowserProfile> FocusedProfiles => FocusedBrowser == null
        ? Array.Empty<BrowserProfile>()
        : FocusedBrowser.Profiles;
    public BrowserProfile? FocusedProfile => FocusedBrowser?.SelectedProfile;
    public string TimerMessage => $"Opening the selected browser in {TimeRemaining} second{(TimeRemaining == 1 ? string.Empty : "s")}.";

    public BrowserInfo? SelectedBrowser
    {
        get => _selectedBrowser;
        set
        {
            if (ReferenceEquals(_selectedBrowser, value))
                return;

            if (_selectedBrowser != null)
                _selectedBrowser.IsSelected = false;

            if (SetProperty(ref _selectedBrowser, value))
            {
                if (_selectedBrowser != null)
                    _selectedBrowser.IsSelected = true;

                StopTimer();
                LaunchCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public event Action? RequestClose;
    public event Action? OpenSettingsRequested;
    public event Action? OpenSitesSettingsRequested;
    public event Action? RequestListFocus;

    public async Task InitializeAsync(string url)
    {
        Url = url;
        MatchOptions.Clear();
        foreach (var option in _rememberedSiteService.BuildMatchOptions(url))
            MatchOptions.Add(option);

        SelectedMatchOption = MatchOptions.LastOrDefault() ?? MatchOptions.FirstOrDefault();
        StatusMessage = "Loading browsers...";

        await ReloadDisplaySettingsAsync();
        await LoadBrowsersAsync();
        await SelectDefaultBrowserAsync();
        await RestoreFocusedBrowserAsync();
        await StartAutoSelectTimerAsync();

        StatusMessage = Browsers.Count == 0
            ? "No browsers were found. Refresh after installing a browser."
            : "Choose a browser or wait for auto-selection.";
        RequestListFocus?.Invoke();
    }

    public async Task ReloadDisplaySettingsAsync()
    {
        var settings = await _configurationService.GetSettingsAsync();
        ShowSettingsButton = settings.ShowSettingsButton;
    }

    public void OnUserInteraction()
    {
        if (IsTimerActive)
            StopTimer();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        StopTimer();
        StatusMessage = "Refreshing browser list...";

        try
        {
            await _browserDetectionService.RefreshBrowserCacheAsync();
            await LoadBrowsersAsync();
            await SelectDefaultBrowserAsync();
            await RestoreFocusedBrowserAsync();
            StatusMessage = "Browser list refreshed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not refresh browsers: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanLaunch))]
    private async Task LaunchAsync()
    {
        if (SelectedBrowser == null)
            return;

        StopTimer();
        StatusMessage = "Opening link...";

        try
        {
            await _configurationService.SaveLastSelectedBrowserAsync(
                SelectedBrowser.Name,
                SelectedBrowser.SelectedProfile?.Name ?? string.Empty);

            if (RememberSelection && SelectedMatchOption != null)
            {
                await _configurationService.SaveRememberedSiteRuleAsync(new RememberedSiteRule
                {
                    Pattern = SelectedMatchOption.Pattern,
                    MatchType = SelectedMatchOption.MatchType,
                    BrowserName = SelectedBrowser.Name,
                    BrowserDisplayName = SelectedBrowser.DisplayName,
                    ProfileName = SelectedBrowser.SelectedProfile?.Name ?? string.Empty
                });
            }

            if (await _browserLauncherService.LaunchBrowserAsync(SelectedBrowser, Url))
                RequestClose?.Invoke();
            else
                StatusMessage = "The selected browser could not be started.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open the link: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    [RelayCommand]
    private void OpenSettings() => OpenSettingsRequested?.Invoke();

    [RelayCommand]
    private void OpenSitesSettings() => OpenSitesSettingsRequested?.Invoke();

    [RelayCommand]
    private async Task FocusBrowserAsync(BrowserInfo? browser)
    {
        if (browser == null)
            return;

        FocusedBrowser = browser;
        SelectedBrowser = browser;
        StopTimer();
        await _configurationService.SaveFocusedBrowserAsync(browser.Name);
        StatusMessage = "This focused browser view will be used next time.";
    }

    [RelayCommand]
    private async Task ShowOtherBrowsersAsync()
    {
        FocusedBrowser = null;
        StopTimer();
        await _configurationService.SaveFocusedBrowserAsync(string.Empty);
        StatusMessage = "Choose a browser.";
        RequestListFocus?.Invoke();
    }

    [RelayCommand]
    private void SelectFocusedProfile(BrowserProfile? profile)
    {
        if (FocusedBrowser == null || profile == null)
            return;

        FocusedBrowser.SelectedProfile = profile;
        SelectedBrowser = FocusedBrowser;
        OnPropertyChanged(nameof(FocusedProfile));
        StopTimer();
    }

    private async Task LoadBrowsersAsync()
    {
        try
        {
            var browsers = await _browserDetectionService.GetAvailableBrowsersAsync();
            if (browsers.Count == 0)
            {
                StatusMessage = "No browsers found. Refreshing detection...";
                await _browserDetectionService.RefreshBrowserCacheAsync();
                browsers = await _browserDetectionService.GetAvailableBrowsersAsync();
            }

            var iconTasks = browsers.Select(browser => _browserIconService.GetIconPathAsync(browser.ExecutablePath)).ToArray();
            var icons = await Task.WhenAll(iconTasks);

            Browsers.Clear();
            for (var index = 0; index < browsers.Count; index++)
            {
                var browser = browsers[index];
                if (browser.SelectedProfile == null && browser.Profiles.Count > 0)
                {
                    browser.SelectedProfile = browser.Profiles.FirstOrDefault(profile => profile.IsDefault)
                                              ?? browser.Profiles[0];
                }

                browser.IconCachePath = icons[index] ?? string.Empty;
                Browsers.Add(browser);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load browsers: {ex.Message}";
        }
    }

    private async Task SelectDefaultBrowserAsync()
    {
        try
        {
            var settings = await _configurationService.GetSettingsAsync();
            if (!string.IsNullOrEmpty(settings.LastSelectedBrowser))
            {
                var lastBrowser = Browsers.FirstOrDefault(browser =>
                    browser.Name.Equals(settings.LastSelectedBrowser, StringComparison.OrdinalIgnoreCase));

                if (lastBrowser != null)
                {
                    if (!string.IsNullOrEmpty(settings.LastSelectedProfile))
                    {
                        var lastProfile = lastBrowser.Profiles.FirstOrDefault(profile =>
                            profile.Name.Equals(settings.LastSelectedProfile, StringComparison.OrdinalIgnoreCase));
                        if (lastProfile != null)
                            lastBrowser.SelectedProfile = lastProfile;
                    }

                    SelectedBrowser = lastBrowser;
                    return;
                }
            }

            SelectedBrowser = Browsers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not restore the previous browser: {ex.Message}";
            SelectedBrowser = Browsers.FirstOrDefault();
        }
    }

    private async Task RestoreFocusedBrowserAsync()
    {
        var settings = await _configurationService.GetSettingsAsync();
        FocusedBrowser = string.IsNullOrWhiteSpace(settings.FocusedBrowserName)
            ? null
            : Browsers.FirstOrDefault(browser =>
                browser.Name.Equals(settings.FocusedBrowserName, StringComparison.OrdinalIgnoreCase));

        if (FocusedBrowser != null)
            SelectedBrowser = FocusedBrowser;
    }

    private async Task StartAutoSelectTimerAsync()
    {
        try
        {
            var settings = await _configurationService.GetSettingsAsync();
            if (!settings.EnableAutoSelect || SelectedBrowser == null)
                return;

            TimeRemaining = settings.AutoSelectTimeoutSeconds;
        }
        catch
        {
            TimeRemaining = 3;
        }

        IsTimerActive = true;
        _autoSelectTimer.Start();
    }

    private void StopTimer()
    {
        _autoSelectTimer.Stop();
        IsTimerActive = false;
        TimeRemaining = 0;
    }

    private async void AutoSelectTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        TimeRemaining--;
        if (TimeRemaining <= 0)
        {
            StopTimer();
            await LaunchAsync();
        }
    }

    private bool CanLaunch() => SelectedBrowser != null;

    public void Dispose()
    {
        _autoSelectTimer.Stop();
        _autoSelectTimer.Tick -= AutoSelectTimer_Tick;
    }
}
