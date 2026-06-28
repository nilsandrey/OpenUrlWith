using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OpenWithTool.Models;

public class BrowserInfo : INotifyPropertyChanged
{
    private bool _isSelected;
    private BrowserProfile? _selectedProfile;
    
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public List<BrowserProfile> Profiles { get; set; } = new();
    public BrowserProfile? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (ReferenceEquals(_selectedProfile, value))
                return;

            _selectedProfile = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FullDisplayName));
        }
    }
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string FullDisplayName => SelectedProfile != null 
        ? $"{DisplayName} - {SelectedProfile.Name}" 
        : DisplayName;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class BrowserProfile
{
    public string Name { get; set; } = string.Empty;
    public string ProfilePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class BrowserCache
{
    public DateTime LastUpdated { get; set; }
    public List<BrowserInfo> Browsers { get; set; } = new();
}

public class AppSettings
{
    public int AutoSelectTimeoutSeconds { get; set; } = 3;
    public int CacheDurationHours { get; set; } = 24;
    public string LastSelectedBrowser { get; set; } = string.Empty;
    public string LastSelectedProfile { get; set; } = string.Empty;
    public string FocusedBrowserName { get; set; } = string.Empty;
    public bool EnableAutoSelect { get; set; } = true;
    public bool ShowSettingsButton { get; set; } = true;
    public List<RememberedSiteRule> RememberedSiteRules { get; set; } = new();
}

public enum SiteMatchType
{
    ExactUrl,
    Domain,
    Path
}

public class SiteMatchOption
{
    public SiteMatchType MatchType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string DisplayText => $"{DisplayName} ({Pattern})";
}

public class RememberedSiteRule : INotifyPropertyChanged
{
    private string _pattern = string.Empty;
    private SiteMatchType _matchType;
    private string _browserName = string.Empty;
    private string _browserDisplayName = string.Empty;
    private string _profileName = string.Empty;

    public string Pattern
    {
        get => _pattern;
        set { _pattern = value; OnPropertyChanged(); }
    }

    public SiteMatchType MatchType
    {
        get => _matchType;
        set { _matchType = value; OnPropertyChanged(); }
    }

    public string BrowserName
    {
        get => _browserName;
        set { _browserName = value; OnPropertyChanged(); }
    }

    public string BrowserDisplayName
    {
        get => string.IsNullOrWhiteSpace(_browserDisplayName) ? BrowserName : _browserDisplayName;
        set { _browserDisplayName = value; OnPropertyChanged(); }
    }

    public string ProfileName
    {
        get => _profileName;
        set { _profileName = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
