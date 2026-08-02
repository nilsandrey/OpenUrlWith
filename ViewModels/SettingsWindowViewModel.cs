using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenWithTool.Models;
using OpenWithTool.Services;

namespace OpenWithTool.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly IUrlProtocolRegistrationService _urlProtocolRegistrationService;
    private AppSettings _settings = new();

    [ObservableProperty]
    public partial bool IsRegisteredAsDefaultBrowser { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    public SettingsWindowViewModel(
        IConfigurationService configurationService,
        IUrlProtocolRegistrationService urlProtocolRegistrationService)
    {
        _configurationService = configurationService;
        _urlProtocolRegistrationService = urlProtocolRegistrationService;
    }

    public int AutoSelectTimeoutSeconds
    {
        get => _settings.AutoSelectTimeoutSeconds;
        set
        {
            var constrainedValue = Math.Clamp(value, 1, 30);
            if (_settings.AutoSelectTimeoutSeconds == constrainedValue)
                return;

            _settings.AutoSelectTimeoutSeconds = constrainedValue;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AutoSelectTimeoutSecondsValue));
        }
    }

    public double AutoSelectTimeoutSecondsValue
    {
        get => AutoSelectTimeoutSeconds;
        set => AutoSelectTimeoutSeconds = (int)Math.Round(value);
    }

    public int CacheDurationHours
    {
        get => _settings.CacheDurationHours;
        set
        {
            var constrainedValue = Math.Clamp(value, 1, 168);
            if (_settings.CacheDurationHours == constrainedValue)
                return;

            _settings.CacheDurationHours = constrainedValue;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CacheDurationHoursValue));
        }
    }

    public double CacheDurationHoursValue
    {
        get => CacheDurationHours;
        set => CacheDurationHours = (int)Math.Round(value);
    }

    public bool EnableAutoSelect
    {
        get => _settings.EnableAutoSelect;
        set
        {
            if (_settings.EnableAutoSelect == value)
                return;

            _settings.EnableAutoSelect = value;
            OnPropertyChanged();
        }
    }

    public bool ShowSettingsButton
    {
        get => _settings.ShowSettingsButton;
        set
        {
            if (_settings.ShowSettingsButton == value)
                return;

            _settings.ShowSettingsButton = value;
            OnPropertyChanged();
        }
    }

    public bool IsRunningAsAdministrator => _urlProtocolRegistrationService.IsRunningAsAdministrator();
    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        try
        {
            _settings = CloneSettings(await _configurationService.GetSettingsAsync());
            OnPropertyChanged(nameof(AutoSelectTimeoutSeconds));
            OnPropertyChanged(nameof(AutoSelectTimeoutSecondsValue));
            OnPropertyChanged(nameof(CacheDurationHours));
            OnPropertyChanged(nameof(CacheDurationHoursValue));
            OnPropertyChanged(nameof(EnableAutoSelect));
            OnPropertyChanged(nameof(ShowSettingsButton));
            IsRegisteredAsDefaultBrowser = _urlProtocolRegistrationService.IsRegisteredAsDefaultBrowser();
            StatusMessage = "Settings are up to date.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            await _configurationService.SaveSettingsAsync(_settings);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not save settings: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    [RelayCommand]
    private async Task RegisterBrowserAsync()
    {
        StatusMessage = "Registering OpenWith Tool...";
        try
        {
            var success = await _urlProtocolRegistrationService.RegisterElevatedAsync();
            IsRegisteredAsDefaultBrowser = success || _urlProtocolRegistrationService.IsRegisteredAsDefaultBrowser();
            StatusMessage = success
                ? "Registered. Choose OpenWith Tool in Windows Default apps."
                : "Browser registration failed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not register the app: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UnregisterBrowserAsync()
    {
        StatusMessage = "Removing browser registration...";
        try
        {
            var success = await _urlProtocolRegistrationService.UnregisterElevatedAsync();
            IsRegisteredAsDefaultBrowser = !success && _urlProtocolRegistrationService.IsRegisteredAsDefaultBrowser();
            StatusMessage = success ? "Browser registration removed." : "Browser unregistration failed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not unregister the app: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenDefaultApps()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:defaultapps",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open Windows Settings: {ex.Message}";
        }
    }

    private static AppSettings CloneSettings(AppSettings settings)
    {
        return new AppSettings
        {
            AutoSelectTimeoutSeconds = settings.AutoSelectTimeoutSeconds,
            CacheDurationHours = settings.CacheDurationHours,
            LastSelectedBrowser = settings.LastSelectedBrowser,
            LastSelectedProfile = settings.LastSelectedProfile,
            FocusedBrowserName = settings.FocusedBrowserName,
            EnableAutoSelect = settings.EnableAutoSelect,
            ShowSettingsButton = settings.ShowSettingsButton,
            RememberedSiteRules = new List<RememberedSiteRule>(settings.RememberedSiteRules)
        };
    }

}
