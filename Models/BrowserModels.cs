using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace OpenWithTool.Models;

public sealed class BrowserInfo : ObservableObject
{
    private bool _isSelected;
    private BrowserProfile? _selectedProfile;
    private string _iconCachePath = string.Empty;

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

            if (_selectedProfile != null)
                _selectedProfile.IsSelected = false;

            if (SetProperty(ref _selectedProfile, value))
            {
                if (_selectedProfile != null)
                    _selectedProfile.IsSelected = true;

                OnPropertyChanged(nameof(FullDisplayName));
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    [JsonIgnore]
    public string IconCachePath
    {
        get => _iconCachePath;
        set => SetProperty(ref _iconCachePath, value);
    }

    [JsonIgnore]
    public string FullDisplayName => SelectedProfile != null
        ? $"{DisplayName} - {SelectedProfile.Name}"
        : DisplayName;
}

public sealed class BrowserProfile : ObservableObject
{
    private bool _isSelected;

    public string Name { get; set; } = string.Empty;
    public string ProfilePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public sealed class BrowserCache
{
    public DateTime LastUpdated { get; set; }
    public List<BrowserInfo> Browsers { get; set; } = new();
}

public sealed class AppSettings
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

public sealed class SiteMatchOption
{
    public SiteMatchType MatchType { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string DisplayText => $"{DisplayName} ({Pattern})";
}

public sealed class SiteMatchTypeOption
{
    public SiteMatchType Value { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class RememberedSiteRule : ObservableObject
{
    private string _pattern = string.Empty;
    private SiteMatchType _matchType;
    private string _browserName = string.Empty;
    private string _browserDisplayName = string.Empty;
    private string _profileName = string.Empty;

    public string Pattern
    {
        get => _pattern;
        set => SetProperty(ref _pattern, value);
    }

    public SiteMatchType MatchType
    {
        get => _matchType;
        set => SetProperty(ref _matchType, value);
    }

    public string BrowserName
    {
        get => _browserName;
        set => SetProperty(ref _browserName, value);
    }

    public string BrowserDisplayName
    {
        get => string.IsNullOrWhiteSpace(_browserDisplayName) ? BrowserName : _browserDisplayName;
        set => SetProperty(ref _browserDisplayName, value);
    }

    public string ProfileName
    {
        get => _profileName;
        set => SetProperty(ref _profileName, value);
    }
}
