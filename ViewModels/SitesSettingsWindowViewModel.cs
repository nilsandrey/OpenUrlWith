using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenWithTool.Models;
using OpenWithTool.Services;

namespace OpenWithTool.ViewModels;

public partial class SitesSettingsWindowViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly IBrowserDetectionService _browserDetectionService;
    private BrowserInfo? _newRuleBrowser;
    private BrowserProfile? _newRuleProfile;
    private RememberedSiteRule? _selectedRule;

    [ObservableProperty]
    public partial SiteMatchTypeOption? NewRuleMatchType { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewRuleUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasError { get; set; }

    public SitesSettingsWindowViewModel(
        IConfigurationService configurationService,
        IBrowserDetectionService browserDetectionService)
    {
        _configurationService = configurationService;
        _browserDetectionService = browserDetectionService;
        Rules = new ObservableCollection<RememberedSiteRule>();
        Browsers = new ObservableCollection<BrowserInfo>();
        MatchTypes = new ObservableCollection<SiteMatchTypeOption>
        {
            new() { Value = SiteMatchType.ExactUrl, DisplayName = "Exact URL" },
            new() { Value = SiteMatchType.Domain, DisplayName = "Entire domain" },
            new() { Value = SiteMatchType.Path, DisplayName = "First path segment" }
        };
        NewRuleMatchType = MatchTypes[0];
    }

    public ObservableCollection<RememberedSiteRule> Rules { get; }
    public ObservableCollection<BrowserInfo> Browsers { get; }
    public ObservableCollection<SiteMatchTypeOption> MatchTypes { get; }
    public bool HasSelectedRule => SelectedRule != null;
    public IReadOnlyList<BrowserProfile> NewRuleProfiles => NewRuleBrowser == null
        ? Array.Empty<BrowserProfile>()
        : NewRuleBrowser.Profiles;

    public RememberedSiteRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetProperty(ref _selectedRule, value))
            {
                OnPropertyChanged(nameof(HasSelectedRule));
                RemoveSelectedRuleCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public BrowserInfo? NewRuleBrowser
    {
        get => _newRuleBrowser;
        set
        {
            if (SetProperty(ref _newRuleBrowser, value))
            {
                NewRuleProfile = value?.Profiles.FirstOrDefault(profile => profile.IsDefault)
                                 ?? value?.Profiles.FirstOrDefault();
                OnPropertyChanged(nameof(NewRuleProfiles));
                AddCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public BrowserProfile? NewRuleProfile
    {
        get => _newRuleProfile;
        set => SetProperty(ref _newRuleProfile, value);
    }

    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        try
        {
            var settings = await _configurationService.GetSettingsAsync();
            Rules.Clear();
            foreach (var rule in settings.RememberedSiteRules)
                Rules.Add(rule);

            var browsers = await _browserDetectionService.GetAvailableBrowsersAsync();
            Browsers.Clear();
            foreach (var browser in browsers)
                Browsers.Add(browser);

            NewRuleBrowser = Browsers.FirstOrDefault();
            HasError = false;
            StatusMessage = Rules.Count == 0 ? "No remembered sites yet." : $"{Rules.Count} remembered site rules.";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Could not load remembered sites: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        if (NewRuleBrowser == null || string.IsNullOrWhiteSpace(NewRuleUrl))
            return;

        var matchType = NewRuleMatchType?.Value ?? SiteMatchType.ExactUrl;
        var pattern = BuildPattern(NewRuleUrl.Trim(), matchType);
        var duplicate = Rules.FirstOrDefault(rule => rule.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase));
        if (duplicate != null)
            Rules.Remove(duplicate);

        Rules.Add(new RememberedSiteRule
        {
            Pattern = pattern,
            MatchType = matchType,
            BrowserName = NewRuleBrowser.Name,
            BrowserDisplayName = NewRuleBrowser.DisplayName,
            ProfileName = NewRuleProfile?.Name ?? string.Empty
        });

        NewRuleUrl = string.Empty;
        await SaveAsync();
        HasError = false;
        StatusMessage = "Remembered site added.";
    }

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private async Task RemoveSelectedRuleAsync()
    {
        if (SelectedRule == null)
            return;

        Rules.Remove(SelectedRule);
        SelectedRule = null;
        await SaveAsync();
        HasError = false;
        StatusMessage = "Remembered site removed.";
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        await SaveAsync();
        RequestClose?.Invoke();
    }

    private async Task SaveAsync()
    {
        try
        {
            var settings = await _configurationService.GetSettingsAsync();
            settings.RememberedSiteRules = Rules.ToList();
            await _configurationService.SaveSettingsAsync(settings);
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Could not save remembered sites: {ex.Message}";
        }
    }

    private bool CanAdd() => NewRuleBrowser != null && !string.IsNullOrWhiteSpace(NewRuleUrl);
    private bool CanRemove() => SelectedRule != null;

    private static string BuildPattern(string url, SiteMatchType matchType)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url;

        var root = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        if (matchType == SiteMatchType.Domain)
            return $"{root}/*";

        if (matchType == SiteMatchType.Path)
        {
            var firstSegment = uri.Segments.Skip(1).FirstOrDefault()?.Trim('/');
            return string.IsNullOrWhiteSpace(firstSegment) ? $"{root}/*" : $"{root}/{firstSegment}/*";
        }

        return uri.AbsoluteUri;
    }
}
