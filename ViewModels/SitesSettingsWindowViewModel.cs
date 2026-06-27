using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using OpenWithTool.Models;
using OpenWithTool.Services;

namespace OpenWithTool.ViewModels;

public class SitesSettingsWindowViewModel : INotifyPropertyChanged
{
    private readonly IConfigurationService _configurationService;
    private readonly IBrowserDetectionService _browserDetectionService;
    private BrowserInfo? _newRuleBrowser;
    private BrowserProfile? _newRuleProfile;
    private SiteMatchType _newRuleMatchType;
    private string _newRuleUrl = string.Empty;
    private RememberedSiteRule? _selectedRule;

    public SitesSettingsWindowViewModel(IConfigurationService configurationService, IBrowserDetectionService browserDetectionService)
    {
        _configurationService = configurationService;
        _browserDetectionService = browserDetectionService;
        Rules = new ObservableCollection<RememberedSiteRule>();
        Browsers = new ObservableCollection<BrowserInfo>();
        MatchTypes = new ObservableCollection<SiteMatchType>(Enum.GetValues<SiteMatchType>());
        AddCommand = new RelayCommand(async () => await AddAsync(), CanAdd);
        RemoveCommand = new RelayCommand(async () => await RemoveAsync(), () => SelectedRule != null);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
    }

    public ObservableCollection<RememberedSiteRule> Rules { get; }
    public ObservableCollection<BrowserInfo> Browsers { get; }
    public ObservableCollection<SiteMatchType> MatchTypes { get; }

    public RememberedSiteRule? SelectedRule
    {
        get => _selectedRule;
        set { _selectedRule = value; OnPropertyChanged(); ((RelayCommand)RemoveCommand).RaiseCanExecuteChanged(); }
    }

    public string NewRuleUrl
    {
        get => _newRuleUrl;
        set { _newRuleUrl = value; OnPropertyChanged(); ((RelayCommand)AddCommand).RaiseCanExecuteChanged(); }
    }

    public SiteMatchType NewRuleMatchType
    {
        get => _newRuleMatchType;
        set { _newRuleMatchType = value; OnPropertyChanged(); }
    }

    public BrowserInfo? NewRuleBrowser
    {
        get => _newRuleBrowser;
        set
        {
            _newRuleBrowser = value;
            NewRuleProfile = value?.Profiles.FirstOrDefault(p => p.IsDefault) ?? value?.Profiles.FirstOrDefault();
            OnPropertyChanged();
            ((RelayCommand)AddCommand).RaiseCanExecuteChanged();
        }
    }

    public BrowserProfile? NewRuleProfile
    {
        get => _newRuleProfile;
        set { _newRuleProfile = value; OnPropertyChanged(); }
    }

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand SaveCommand { get; }
    public event Action? RequestClose;
    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync()
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
    }

    private async Task AddAsync()
    {
        if (NewRuleBrowser == null || string.IsNullOrWhiteSpace(NewRuleUrl))
            return;

        var pattern = BuildPattern(NewRuleUrl.Trim(), NewRuleMatchType);
        Rules.Remove(Rules.FirstOrDefault(r => r.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase))!);
        Rules.Add(new RememberedSiteRule
        {
            Pattern = pattern,
            MatchType = NewRuleMatchType,
            BrowserName = NewRuleBrowser.Name,
            BrowserDisplayName = NewRuleBrowser.DisplayName,
            ProfileName = NewRuleProfile?.Name ?? string.Empty
        });
        NewRuleUrl = string.Empty;
        await SaveAsync(false);
    }

    private async Task RemoveAsync()
    {
        if (SelectedRule != null)
            Rules.Remove(SelectedRule);
        await SaveAsync(false);
    }

    private async Task SaveAsync(bool close = true)
    {
        var settings = await _configurationService.GetSettingsAsync();
        settings.RememberedSiteRules = Rules.ToList();
        await _configurationService.SaveSettingsAsync(settings);
        if (close)
            RequestClose?.Invoke();
    }

    private bool CanAdd() => NewRuleBrowser != null && !string.IsNullOrWhiteSpace(NewRuleUrl);

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

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
