using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using OpenWithTool.Models;
using OpenWithTool.Services;

namespace OpenWithTool.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IBrowserDetectionService _browserDetectionService;
    private readonly IConfigurationService _configurationService;
    private readonly IBrowserLauncherService _browserLauncherService;
    private readonly DispatcherTimer _autoSelectTimer;
    
    private string _url = string.Empty;
    private BrowserInfo? _selectedBrowser;
    private int _timeRemaining;
    private bool _isTimerActive;
    private string _statusMessage = string.Empty;

    public MainWindowViewModel(
        IBrowserDetectionService browserDetectionService,
        IConfigurationService configurationService,
        IBrowserLauncherService browserLauncherService)
    {
        _browserDetectionService = browserDetectionService;
        _configurationService = configurationService;
        _browserLauncherService = browserLauncherService;

        Browsers = new ObservableCollection<BrowserInfo>();
        
        _autoSelectTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _autoSelectTimer.Tick += AutoSelectTimer_Tick;

        // Commands
        RefreshCommand = new RelayCommand(async () => await RefreshBrowsersAsync());
        LaunchCommand = new RelayCommand(async () => await LaunchSelectedBrowserAsync(), CanLaunch);
        CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
        SettingsCommand = new RelayCommand(() => OpenSettings?.Invoke());
    }

    public ObservableCollection<BrowserInfo> Browsers { get; }

    public string Url
    {
        get => _url;
        set
        {
            _url = value;
            OnPropertyChanged();
        }
    }

    public BrowserInfo? SelectedBrowser
    {
        get => _selectedBrowser;
        set
        {
            if (_selectedBrowser != null)
                _selectedBrowser.IsSelected = false;
            
            _selectedBrowser = value;
            
            if (_selectedBrowser != null)
                _selectedBrowser.IsSelected = true;
            
            OnPropertyChanged();
            StopTimer();
            ((RelayCommand)LaunchCommand).RaiseCanExecuteChanged();
        }
    }

    public int TimeRemaining
    {
        get => _timeRemaining;
        set
        {
            _timeRemaining = value;
            OnPropertyChanged();
        }
    }

    public bool IsTimerActive
    {
        get => _isTimerActive;
        set
        {
            _isTimerActive = value;
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

    public ICommand RefreshCommand { get; }
    public ICommand LaunchCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SettingsCommand { get; }

    public event Action? RequestClose;
    public event Action? OpenSettings;
    public event Action? RequestListFocus;

    public async Task InitializeAsync(string url)
    {
        Url = url;
        StatusMessage = "Loading browsers...";
        
        await LoadBrowsersAsync();
        await SelectDefaultBrowserAsync();
        await StartAutoSelectTimerAsync();
        
        StatusMessage = "Select a browser or wait for auto-selection";
        
        // Request focus for the browser list after initialization
        RequestListFocus?.Invoke();
    }

    public void OnUserInteraction()
    {
        StopTimer();
    }

    private async Task LoadBrowsersAsync()
    {
        try
        {
            var browsers = await _browserDetectionService.GetAvailableBrowsersAsync();
            
            // If no browsers found, force refresh the cache and try again
            if (!browsers.Any())
            {
                StatusMessage = "No browsers found, refreshing cache...";
                await _browserDetectionService.RefreshBrowserCacheAsync();
                browsers = await _browserDetectionService.GetAvailableBrowsersAsync();
            }
            
            Browsers.Clear();
            foreach (var browser in browsers)
            {
                // Set default profile if none selected and profiles exist
                if (browser.SelectedProfile == null && browser.Profiles.Any())
                {
                    browser.SelectedProfile = browser.Profiles.FirstOrDefault(p => p.IsDefault) 
                                            ?? browser.Profiles.First();
                }
                Browsers.Add(browser);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading browsers: {ex.Message}";
        }
    }

    private async Task SelectDefaultBrowserAsync()
    {
        try
        {
            var settings = await _configurationService.GetSettingsAsync();
            
            if (!string.IsNullOrEmpty(settings.LastSelectedBrowser))
            {
                var lastBrowser = Browsers.FirstOrDefault(b => 
                    b.Name.Equals(settings.LastSelectedBrowser, StringComparison.OrdinalIgnoreCase));
                
                if (lastBrowser != null)
                {
                    // Try to select the same profile if specified
                    if (!string.IsNullOrEmpty(settings.LastSelectedProfile))
                    {
                        var lastProfile = lastBrowser.Profiles.FirstOrDefault(p => 
                            p.Name.Equals(settings.LastSelectedProfile, StringComparison.OrdinalIgnoreCase));
                        
                        if (lastProfile != null)
                            lastBrowser.SelectedProfile = lastProfile;
                    }
                    
                    SelectedBrowser = lastBrowser;
                    return;
                }
            }
            
            // If no last selection or browser not found, select first browser
            if (Browsers.Any())
            {
                SelectedBrowser = Browsers.First();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error selecting default browser: {ex.Message}";
            if (Browsers.Any())
                SelectedBrowser = Browsers.First();
        }
    }

    private async Task StartAutoSelectTimerAsync()
    {
        try
        {
            var settings = await _configurationService.GetSettingsAsync();
            
            if (!settings.EnableAutoSelect || SelectedBrowser == null)
                return;

            TimeRemaining = settings.AutoSelectTimeoutSeconds;
            IsTimerActive = true;
            _autoSelectTimer.Start();
        }
        catch
        {
            // If settings can't be loaded, use default behavior
            TimeRemaining = 3;
            IsTimerActive = true;
            _autoSelectTimer.Start();
        }
    }

    private void StopTimer()
    {
        _autoSelectTimer.Stop();
        IsTimerActive = false;
        TimeRemaining = 0;
    }

    private async void AutoSelectTimer_Tick(object? sender, EventArgs e)
    {
        TimeRemaining--;
        
        if (TimeRemaining <= 0)
        {
            StopTimer();
            await LaunchSelectedBrowserAsync();
        }
    }

    private async Task RefreshBrowsersAsync()
    {
        StatusMessage = "Refreshing browser list...";
        
        try
        {
            await _browserDetectionService.RefreshBrowserCacheAsync();
            await LoadBrowsersAsync();
            await SelectDefaultBrowserAsync();
            
            StatusMessage = "Browser list refreshed";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing browsers: {ex.Message}";
        }
    }

    private async Task LaunchSelectedBrowserAsync()
    {
        if (SelectedBrowser == null)
            return;

        StopTimer();
        StatusMessage = "Launching browser...";

        try
        {
            // Save the selection for next time
            await _configurationService.SaveLastSelectedBrowserAsync(
                SelectedBrowser.Name,
                SelectedBrowser.SelectedProfile?.Name ?? string.Empty);

            var success = await _browserLauncherService.LaunchBrowserAsync(SelectedBrowser, Url);
            
            if (success)
            {
                RequestClose?.Invoke();
            }
            else
            {
                StatusMessage = "Failed to launch browser";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error launching browser: {ex.Message}";
        }
    }

    private bool CanLaunch()
    {
        return SelectedBrowser != null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}