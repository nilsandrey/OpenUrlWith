using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenWithTool.Models;

namespace OpenWithTool.Services;

public interface IBrowserDetectionService
{
    Task<List<BrowserInfo>> GetAvailableBrowsersAsync(bool forceRefresh = false);
    Task RefreshBrowserCacheAsync();
}

public class BrowserDetectionService : IBrowserDetectionService
{
    private readonly IConfigurationService _configService;
    private readonly string _cacheFilePath;

    public BrowserDetectionService(IConfigurationService configService)
    {
        _configService = configService;
        _cacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenWithTool",
            "browser_cache.json");
        
        Directory.CreateDirectory(Path.GetDirectoryName(_cacheFilePath)!);
    }

    public async Task<List<BrowserInfo>> GetAvailableBrowsersAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && await IsCacheValidAsync())
        {
            var cachedBrowsers = await LoadCacheAsync();
            if (cachedBrowsers != null)
                return NormalizeBrowsers(cachedBrowsers);
        }

        var browsers = NormalizeBrowsers(await DetectBrowsersAsync());
        await SaveCacheAsync(browsers);
        return browsers;
    }

    public async Task RefreshBrowserCacheAsync()
    {
        await GetAvailableBrowsersAsync(true);
    }

    private async Task<bool> IsCacheValidAsync()
    {
        if (!File.Exists(_cacheFilePath))
            return false;

        try
        {
            var cacheJson = await File.ReadAllTextAsync(_cacheFilePath);
            var cache = JsonConvert.DeserializeObject<BrowserCache>(cacheJson);
            
            if (cache == null)
                return false;

            var settings = await _configService.GetSettingsAsync();
            var cacheAge = DateTime.Now - cache.LastUpdated;
            return cacheAge.TotalHours < settings.CacheDurationHours;
        }
        catch
        {
            return false;
        }
    }

    private async Task<List<BrowserInfo>?> LoadCacheAsync()
    {
        try
        {
            var cacheJson = await File.ReadAllTextAsync(_cacheFilePath);
            var cache = JsonConvert.DeserializeObject<BrowserCache>(cacheJson);
            return cache?.Browsers;
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveCacheAsync(List<BrowserInfo> browsers)
    {
        try
        {
            var cache = new BrowserCache
            {
                LastUpdated = DateTime.Now,
                Browsers = browsers
            };

            var cacheJson = JsonConvert.SerializeObject(cache, Formatting.Indented);
            await File.WriteAllTextAsync(_cacheFilePath, cacheJson);
        }
        catch
        {
            // Ignore cache save errors
        }
    }

    private async Task<List<BrowserInfo>> DetectBrowsersAsync()
    {
        var browsers = new List<BrowserInfo>();

        // Add known browsers
        await AddBrowserIfInstalled(browsers, DetectChrome());
        await AddBrowserIfInstalled(browsers, DetectFirefox());
        await AddBrowserIfInstalled(browsers, DetectEdge());
        await AddBrowserIfInstalled(browsers, DetectOpera());
        await AddBrowserIfInstalled(browsers, DetectBrave());
        await AddBrowserIfInstalled(browsers, DetectVivaldi());

        // Detect other browsers from registry
        await DetectBrowsersFromRegistry(browsers);

        return NormalizeBrowsers(browsers);
    }

    private static List<BrowserInfo> NormalizeBrowsers(List<BrowserInfo> browsers)
    {
        return browsers
            .Select(RefreshChromiumProfileDisplayNames)
            .Where(b => !IsCurrentToolExecutable(b.ExecutablePath))
            .GroupBy(GetBrowserIdentity, StringComparer.OrdinalIgnoreCase)
            .Select(MergeBrowserGroup)
            .OrderBy(b => b.DisplayName)
            .ToList();
    }

    private static BrowserInfo RefreshChromiumProfileDisplayNames(BrowserInfo browser)
    {
        if (!IsChromiumProfileBrowser(browser) || !browser.Profiles.Any())
            return browser;

        var profilePaths = browser.Profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.ProfilePath) && Directory.Exists(profile.ProfilePath))
            .Select(profile => profile.ProfilePath)
            .ToList();

        var userDataDir = profilePaths
            .Select(profilePath => Directory.GetParent(profilePath)?.FullName)
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        if (userDataDir == null)
            return browser;

        var localStateProfiles = LoadChromiumProfiles(userDataDir);

        foreach (var profile in browser.Profiles.Where(profile =>
                     !string.IsNullOrWhiteSpace(profile.ProfilePath) && Directory.Exists(profile.ProfilePath)))
        {
            var profileDirectoryName = Path.GetFileName(profile.ProfilePath);
            profile.Name = GetChromiumProfileDisplayName(
                profile.ProfilePath,
                profileDirectoryName,
                localStateProfiles);
            profile.Arguments = GetChromiumProfileArguments(profileDirectoryName);
        }

        browser.Profiles = GetProfilesWithUniqueDisplayNames(
            FilterChromeWildcardProfiles(browser, browser.Profiles, localStateProfiles)).ToList();

        if (browser.SelectedProfile != null && !browser.Profiles.Contains(browser.SelectedProfile))
        {
            browser.SelectedProfile = browser.Profiles.FirstOrDefault(profile => profile.IsDefault)
                                      ?? browser.Profiles.FirstOrDefault();
        }

        return browser;
    }

    private static bool IsChromiumProfileBrowser(BrowserInfo browser)
    {
        return browser.Name.Equals("chrome", StringComparison.OrdinalIgnoreCase)
               || browser.Name.Equals("edge", StringComparison.OrdinalIgnoreCase)
               || browser.Name.Equals("brave", StringComparison.OrdinalIgnoreCase)
               || browser.Name.Equals("vivaldi", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCurrentToolExecutable(string executablePath)
    {
        var browserPath = NormalizeExecutablePath(executablePath);
        if (browserPath == null)
            return false;

        return GetCurrentToolExecutablePaths().Any(toolPath =>
            string.Equals(toolPath, browserPath, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> GetCurrentToolExecutablePaths()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var assemblyLocation = entryAssembly?.Location;
        var assemblyName = entryAssembly?.GetName().Name;

        var candidates = new[]
        {
            Environment.ProcessPath,
            assemblyLocation,
            string.IsNullOrWhiteSpace(assemblyLocation) ? null : Path.ChangeExtension(assemblyLocation, ".exe"),
            string.IsNullOrWhiteSpace(assemblyName) ? null : Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.exe")
        };

        return candidates
            .Select(NormalizeExecutablePath)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? NormalizeExecutablePath(string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
            return null;

        try
        {
            return Path.GetFullPath(executablePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return null;
        }
    }

    private static string GetBrowserIdentity(BrowserInfo browser)
    {
        if (!string.IsNullOrWhiteSpace(browser.ExecutablePath))
            return NormalizeExecutablePath(browser.ExecutablePath)?.ToUpperInvariant() ?? browser.ExecutablePath;

        return !string.IsNullOrWhiteSpace(browser.Name)
            ? browser.Name
            : browser.DisplayName;
    }

    private static BrowserInfo MergeBrowserGroup(IEnumerable<BrowserInfo> browserGroup)
    {
        var browsers = browserGroup.ToList();
        var primary = browsers
            .OrderByDescending(b => b.Profiles.Count)
            .ThenByDescending(b => b.Profiles.Count(p => !string.IsNullOrWhiteSpace(p.ProfilePath)))
            .First();

        foreach (var browser in browsers.Where(b => !ReferenceEquals(b, primary)))
        {
            foreach (var profile in browser.Profiles.Where(profile =>
                         !primary.Profiles.Any(existingProfile => AreSameProfile(existingProfile, profile))))
                primary.Profiles.Add(profile);
        }

        if (primary.SelectedProfile == null && primary.Profiles.Any())
        {
            primary.SelectedProfile = primary.Profiles.FirstOrDefault(p => p.IsDefault)
                                      ?? primary.Profiles[0];
        }

        return primary;
    }

    private static bool AreSameProfile(BrowserProfile first, BrowserProfile second)
    {
        if (!string.IsNullOrWhiteSpace(first.ProfilePath) && !string.IsNullOrWhiteSpace(second.ProfilePath))
        {
            return Path.GetFullPath(first.ProfilePath).Equals(
                Path.GetFullPath(second.ProfilePath),
                StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(first.Arguments, second.Arguments, StringComparison.OrdinalIgnoreCase)
               || string.Equals(first.Name, second.Name, StringComparison.OrdinalIgnoreCase);
    }

    private async Task AddBrowserIfInstalled(List<BrowserInfo> browsers, Task<BrowserInfo?> browserTask)
    {
        var browser = await browserTask;
        if (browser != null && File.Exists(browser.ExecutablePath))
        {
            browsers.Add(browser);
        }
    }

    private Task<BrowserInfo?> DetectChrome()
    {
        return Task.Run(() =>
        {
            var chromePaths = new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\Application\chrome.exe")
            };

            var chromePath = chromePaths.FirstOrDefault(File.Exists);
            if (chromePath == null) return null;

            var browser = new BrowserInfo
            {
                Name = "chrome",
                DisplayName = "Google Chrome",
                ExecutablePath = chromePath,
                IconPath = chromePath
            };

            // Detect Chrome profiles
            var userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Google\Chrome\User Data");

            DetectChromeProfiles(browser, userDataDir);
            return browser;
        });
    }

    private Task<BrowserInfo?> DetectFirefox()
    {
        return Task.Run(() =>
        {
            var firefoxPaths = new[]
            {
                @"C:\Program Files\Mozilla Firefox\firefox.exe",
                @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"
            };

            var firefoxPath = firefoxPaths.FirstOrDefault(File.Exists);
            if (firefoxPath == null) return null;

            var browser = new BrowserInfo
            {
                Name = "firefox",
                DisplayName = "Mozilla Firefox",
                ExecutablePath = firefoxPath,
                IconPath = firefoxPath
            };

            // Detect Firefox profiles
            var profilesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Mozilla\Firefox\Profiles");

            DetectFirefoxProfiles(browser, profilesDir);
            return browser;
        });
    }

    private Task<BrowserInfo?> DetectEdge()
    {
        return Task.Run(() =>
        {
            var edgePaths = new[]
            {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
            };

            var edgePath = edgePaths.FirstOrDefault(File.Exists);
            if (edgePath == null) return null;

            var browser = new BrowserInfo
            {
                Name = "edge",
                DisplayName = "Microsoft Edge",
                ExecutablePath = edgePath,
                IconPath = edgePath
            };

            // Detect Edge profiles
            var userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\Edge\User Data");

            DetectChromeProfiles(browser, userDataDir); // Edge uses Chrome profile structure
            return browser;
        });
    }

    private Task<BrowserInfo?> DetectOpera()
    {
        return Task.Run(() =>
        {
            var operaPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Opera\opera.exe"),
                @"C:\Program Files\Opera\opera.exe",
                @"C:\Program Files (x86)\Opera\opera.exe"
            };

            var operaPath = operaPaths.FirstOrDefault(File.Exists);
            if (operaPath == null) return null;

            return new BrowserInfo
            {
                Name = "opera",
                DisplayName = "Opera",
                ExecutablePath = operaPath,
                IconPath = operaPath,
                Profiles = new List<BrowserProfile>
                {
                    new() { Name = "Default", IsDefault = true, Arguments = "" }
                }
            };
        });
    }

    private Task<BrowserInfo?> DetectBrave()
    {
        return Task.Run(() =>
        {
            var bravePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"BraveSoftware\Brave-Browser\Application\brave.exe"),
                @"C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe",
                @"C:\Program Files (x86)\BraveSoftware\Brave-Browser\Application\brave.exe"
            };

            var bravePath = bravePaths.FirstOrDefault(File.Exists);
            if (bravePath == null) return null;

            var browser = new BrowserInfo
            {
                Name = "brave",
                DisplayName = "Brave Browser",
                ExecutablePath = bravePath,
                IconPath = bravePath
            };

            // Detect Brave profiles (uses Chrome structure)
            var userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"BraveSoftware\Brave-Browser\User Data");

            DetectChromeProfiles(browser, userDataDir);
            return browser;
        });
    }

    private Task<BrowserInfo?> DetectVivaldi()
    {
        return Task.Run(() =>
        {
            var vivaldiPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Vivaldi\Application\vivaldi.exe"),
                @"C:\Program Files\Vivaldi\Application\vivaldi.exe",
                @"C:\Program Files (x86)\Vivaldi\Application\vivaldi.exe"
            };

            var vivaldiPath = vivaldiPaths.FirstOrDefault(File.Exists);
            if (vivaldiPath == null) return null;

            var browser = new BrowserInfo
            {
                Name = "vivaldi",
                DisplayName = "Vivaldi",
                ExecutablePath = vivaldiPath,
                IconPath = vivaldiPath
            };

            // Detect Vivaldi profiles (uses Chrome structure)
            var userDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Vivaldi\User Data");

            DetectChromeProfiles(browser, userDataDir);
            return browser;
        });
    }

    private void DetectChromeProfiles(BrowserInfo browser, string userDataDir)
    {
        if (!Directory.Exists(userDataDir))
        {
            browser.Profiles.Add(new BrowserProfile { Name = "Default", IsDefault = true, Arguments = "" });
            return;
        }

        var profileDirs = Directory.GetDirectories(userDataDir)
            .Where(d => Path.GetFileName(d).StartsWith("Profile ") || Path.GetFileName(d) == "Default")
            .ToList();

        if (!profileDirs.Any())
        {
            browser.Profiles.Add(new BrowserProfile { Name = "Default", IsDefault = true, Arguments = "" });
            return;
        }

        var localStateProfiles = LoadChromiumProfiles(userDataDir);
        var detectedProfiles = new List<BrowserProfile>();

        foreach (var profileDir in profileDirs)
        {
            var profileDirectoryName = Path.GetFileName(profileDir);
            var displayName = GetChromiumProfileDisplayName(
                profileDir,
                profileDirectoryName,
                localStateProfiles);

            detectedProfiles.Add(new BrowserProfile
            {
                Name = displayName,
                ProfilePath = profileDir,
                Arguments = GetChromiumProfileArguments(profileDirectoryName),
                IsDefault = profileDirectoryName == "Default"
            });
        }

        foreach (var profile in GetProfilesWithUniqueDisplayNames(
                     FilterChromeWildcardProfiles(browser, detectedProfiles, localStateProfiles)))
            browser.Profiles.Add(profile);
    }

    private static string GetChromiumProfileArguments(string profileDirectoryName)
    {
        return $"--profile-directory=\"{profileDirectoryName}\"";
    }

    private static List<BrowserProfile> FilterChromeWildcardProfiles(
        BrowserInfo browser,
        List<BrowserProfile> profiles,
        Dictionary<string, ChromiumProfileInfo> localStateProfiles)
    {
        if (!browser.Name.Equals("chrome", StringComparison.OrdinalIgnoreCase) || profiles.Count <= 1)
            return profiles;

        return profiles
            .Where(profile => !IsChromeWildcardProfile(profile, localStateProfiles))
            .ToList();
    }

    private static bool IsChromeWildcardProfile(
        BrowserProfile profile,
        Dictionary<string, ChromiumProfileInfo> localStateProfiles)
    {
        var profileDirectoryName = Path.GetFileName(profile.ProfilePath);
        if (!profileDirectoryName.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!localStateProfiles.TryGetValue(profileDirectoryName, out var profileInfo))
            return false;

        return profileInfo.IsUsingDefaultName
               && !profileInfo.HasGaiaName
               && !profileInfo.HasUserName;
    }

    private static Dictionary<string, ChromiumProfileInfo> LoadChromiumProfiles(string userDataDir)
    {
        var localStateFile = Path.Combine(userDataDir, "Local State");
        if (!File.Exists(localStateFile))
            return new Dictionary<string, ChromiumProfileInfo>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var localStateJson = File.ReadAllText(localStateFile);
            var localState = JObject.Parse(localStateJson);
            var infoCache = localState["profile"]?["info_cache"] as JObject;

            if (infoCache == null)
                return new Dictionary<string, ChromiumProfileInfo>(StringComparer.OrdinalIgnoreCase);

            return infoCache.Properties().ToDictionary(
                property => property.Name,
                property => ChromiumProfileInfo.FromJson(property.Value as JObject),
                StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, ChromiumProfileInfo>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string GetChromiumProfileDisplayName(
        string profileDir,
        string profileDirectoryName,
        Dictionary<string, ChromiumProfileInfo> localStateProfiles)
    {
        var preferencesName = GetChromiumPreferencesProfileName(profileDir);
        var candidates = localStateProfiles.TryGetValue(profileDirectoryName, out var localStateProfile)
            ? localStateProfile.NameCandidates.Concat(new[] { preferencesName, profileDirectoryName })
            : new[] { preferencesName, profileDirectoryName };

        return candidates
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => IsGenericChromiumProfileName(name!) ? 1 : 0)
            .First()!;
    }

    private static string? GetChromiumPreferencesProfileName(string profileDir)
    {
        var prefsFile = Path.Combine(profileDir, "Preferences");
        if (!File.Exists(prefsFile))
            return null;

        try
        {
            var prefsJson = File.ReadAllText(prefsFile);
            var prefs = JObject.Parse(prefsJson);
            return prefs["profile"]?["name"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsGenericChromiumProfileName(string profileName)
    {
        return profileName.Equals("Default", StringComparison.OrdinalIgnoreCase)
               || profileName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase)
               || profileName.StartsWith("Person ", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<BrowserProfile> GetProfilesWithUniqueDisplayNames(List<BrowserProfile> profiles)
    {
        foreach (var profileGroup in profiles.GroupBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (profileGroup.Count() == 1)
            {
                yield return profileGroup.First();
                continue;
            }

            foreach (var profile in profileGroup)
            {
                var profileDirectoryName = Path.GetFileName(profile.ProfilePath);
                profile.Name = $"{profile.Name} ({profileDirectoryName})";
                yield return profile;
            }
        }
    }

    private sealed class ChromiumProfileInfo
    {
        public List<string> NameCandidates { get; init; } = new();
        public bool IsUsingDefaultName { get; init; }
        public bool HasGaiaName { get; init; }
        public bool HasUserName { get; init; }

        public static ChromiumProfileInfo FromJson(JObject? profileInfo)
        {
            if (profileInfo == null)
                return new ChromiumProfileInfo();

            return new ChromiumProfileInfo
            {
                NameCandidates = GetNameCandidates(profileInfo).ToList(),
                IsUsingDefaultName = profileInfo["is_using_default_name"]?.Value<bool>() == true,
                HasGaiaName = !string.IsNullOrWhiteSpace(profileInfo["gaia_name"]?.ToString()),
                HasUserName = !string.IsNullOrWhiteSpace(profileInfo["user_name"]?.ToString())
            };
        }

        private static IEnumerable<string> GetNameCandidates(JObject profileInfo)
        {
            foreach (var fieldName in new[] { "name", "shortcut_name", "local_profile_name" })
            {
                var value = profileInfo[fieldName]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    yield return value;
            }
        }
    }

    private void DetectFirefoxProfiles(BrowserInfo browser, string profilesDir)
    {
        if (!Directory.Exists(profilesDir))
        {
            browser.Profiles.Add(new BrowserProfile { Name = "Default", IsDefault = true, Arguments = "" });
            return;
        }

        var profileDirs = Directory.GetDirectories(profilesDir);
        if (!profileDirs.Any())
        {
            browser.Profiles.Add(new BrowserProfile { Name = "Default", IsDefault = true, Arguments = "" });
            return;
        }

        foreach (var profileDir in profileDirs)
        {
            var profileName = Path.GetFileName(profileDir);
            // Extract user-friendly name (everything after the first dot)
            var displayName = profileName.Contains('.') 
                ? profileName.Substring(profileName.IndexOf('.') + 1)
                : profileName;

            browser.Profiles.Add(new BrowserProfile
            {
                Name = displayName,
                ProfilePath = profileDir,
                Arguments = $"-profile \"{profileDir}\"",
                IsDefault = profileDirs.Length == 1
            });
        }
    }

    private async Task DetectBrowsersFromRegistry(List<BrowserInfo> browsers)
    {
        await Task.Run(() =>
        {
            try
            {
                // Check registered browsers in Windows
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet");
                if (key == null) return;

                foreach (var browserKeyName in key.GetSubKeyNames())
                {
                    if (browsers.Any(b => b.Name.Equals(browserKeyName, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    using var browserKey = key.OpenSubKey(browserKeyName);
                    if (browserKey == null) continue;

                    using var commandKey = browserKey.OpenSubKey(@"shell\open\command");
                    if (commandKey == null) continue;

                    var command = commandKey.GetValue("")?.ToString();
                    if (string.IsNullOrEmpty(command)) continue;

                    // Extract executable path from command
                    var executablePath = ExtractExecutablePath(command);
                    if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
                        continue;

                    if (IsCurrentToolExecutable(executablePath))
                        continue;

                    var displayName = browserKey.GetValue("")?.ToString() ?? browserKeyName;

                    browsers.Add(new BrowserInfo
                    {
                        Name = browserKeyName.ToLowerInvariant(),
                        DisplayName = displayName,
                        ExecutablePath = executablePath,
                        IconPath = executablePath,
                        Profiles = new List<BrowserProfile>
                        {
                            new() { Name = "Default", IsDefault = true, Arguments = "" }
                        }
                    });
                }
            }
            catch
            {
                // Ignore registry access errors
            }
        });
    }

    private string ExtractExecutablePath(string command)
    {
        command = command.Trim();
        
        if (command.StartsWith("\""))
        {
            var endQuoteIndex = command.IndexOf("\"", 1);
            if (endQuoteIndex > 0)
                return command.Substring(1, endQuoteIndex - 1);
        }
        
        var spaceIndex = command.IndexOf(" ");
        return spaceIndex > 0 ? command.Substring(0, spaceIndex) : command;
    }
}
