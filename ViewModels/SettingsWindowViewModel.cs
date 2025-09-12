using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using OpenWithTool.Models;
using OpenWithTool.Services;

namespace OpenWithTool.ViewModels;

public class SettingsWindowViewModel : INotifyPropertyChanged
{
    private readonly IConfigurationService _configurationService;
    private readonly IUrlProtocolRegistrationService _urlProtocolRegistrationService;
    
    private AppSettings _settings = new();
    private bool _isRegisteredAsDefaultBrowser;
    private string _statusMessage = string.Empty;

    public SettingsWindowViewModel(
        IConfigurationService configurationService,
        IUrlProtocolRegistrationService urlProtocolRegistrationService)
    {
        _configurationService = configurationService;
        _urlProtocolRegistrationService = urlProtocolRegistrationService;

        // Commands
        SaveCommand = new RelayCommand(async () => await SaveSettingsAsync());
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
        RegisterBrowserCommand = new RelayCommand(async () => await RegisterAsBrowserAsync());
        UnregisterBrowserCommand = new RelayCommand(async () => await UnregisterAsBrowserAsync());
        OpenDefaultAppsCommand = new RelayCommand(() => OpenDefaultAppsSettings());
    }

    public int AutoSelectTimeoutSeconds
    {
        get => _settings.AutoSelectTimeoutSeconds;
        set
        {
            _settings.AutoSelectTimeoutSeconds = Math.Max(1, Math.Min(30, value));
            OnPropertyChanged();
        }
    }

    public int CacheDurationHours
    {
        get => _settings.CacheDurationHours;
        set
        {
            _settings.CacheDurationHours = Math.Max(1, Math.Min(168, value)); // 1 hour to 1 week
            OnPropertyChanged();
        }
    }

    public bool EnableAutoSelect
    {
        get => _settings.EnableAutoSelect;
        set
        {
            _settings.EnableAutoSelect = value;
            OnPropertyChanged();
        }
    }

    public bool ShowSettingsButton
    {
        get => _settings.ShowSettingsButton;
        set
        {
            _settings.ShowSettingsButton = value;
            OnPropertyChanged();
        }
    }

    public bool IsRegisteredAsDefaultBrowser
    {
        get => _isRegisteredAsDefaultBrowser;
        set
        {
            _isRegisteredAsDefaultBrowser = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsRunningAsAdministrator => _urlProtocolRegistrationService.IsRunningAsAdministrator();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand RegisterBrowserCommand { get; }
    public ICommand UnregisterBrowserCommand { get; }
    public ICommand OpenDefaultAppsCommand { get; }

    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        try
        {
            _settings = await _configurationService.GetSettingsAsync();
            
            // Notify all properties changed
            OnPropertyChanged(nameof(AutoSelectTimeoutSeconds));
            OnPropertyChanged(nameof(CacheDurationHours));
            OnPropertyChanged(nameof(EnableAutoSelect));
            OnPropertyChanged(nameof(ShowSettingsButton));

            // Check registration status
            IsRegisteredAsDefaultBrowser = _urlProtocolRegistrationService.IsRegisteredAsDefaultBrowser();
            
            StatusMessage = "Settings loaded successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading settings: {ex.Message}";
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            await _configurationService.SaveSettingsAsync(_settings);
            StatusMessage = "Settings saved successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving settings: {ex.Message}";
        }
    }

    private Task RegisterAsBrowserAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                if (!IsRunningAsAdministrator)
                {
                    StatusMessage = "Administrator privileges required for browser registration";
                    return;
                }

                var success = _urlProtocolRegistrationService.RegisterAsDefaultBrowser();
                if (success)
                {
                    IsRegisteredAsDefaultBrowser = true;
                    StatusMessage = "Successfully registered as browser. You can now set this as your default browser in Windows Settings.";
                }
                else
                {
                    StatusMessage = "Failed to register as browser";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error registering as browser: {ex.Message}";
            }
        });
    }

    private Task UnregisterAsBrowserAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                if (!IsRunningAsAdministrator)
                {
                    StatusMessage = "Administrator privileges required for browser unregistration";
                    return;
                }

                var success = _urlProtocolRegistrationService.UnregisterAsDefaultBrowser();
                if (success)
                {
                    IsRegisteredAsDefaultBrowser = false;
                    StatusMessage = "Successfully unregistered as browser";
                }
                else
                {
                    StatusMessage = "Failed to unregister as browser";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error unregistering as browser: {ex.Message}";
            }
        });
    }

    private void OpenDefaultAppsSettings()
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
            StatusMessage = $"Failed to open Windows Settings: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}