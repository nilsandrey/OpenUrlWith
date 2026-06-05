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
    public bool EnableAutoSelect { get; set; } = true;
    public bool ShowSettingsButton { get; set; } = true;
}