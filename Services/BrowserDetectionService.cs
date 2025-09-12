using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Newtonsoft.Json;
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
                return cachedBrowsers;
        }

        var browsers = await DetectBrowsersAsync();
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

        return browsers.OrderBy(b => b.DisplayName).ToList();
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

        foreach (var profileDir in profileDirs)
        {
            var profileName = Path.GetFileName(profileDir);
            var prefsFile = Path.Combine(profileDir, "Preferences");
            
            var displayName = profileName;
            if (File.Exists(prefsFile))
            {
                try
                {
                    var prefsJson = File.ReadAllText(prefsFile);
                    dynamic prefs = JsonConvert.DeserializeObject(prefsJson)!;
                    if (prefs?.profile?.name != null)
                    {
                        displayName = prefs.profile.name.ToString();
                    }
                }
                catch
                {
                    // Use folder name if prefs can't be read
                }
            }

            var arguments = profileName == "Default" ? "" : $"--profile-directory=\"{profileName}\"";
            
            browser.Profiles.Add(new BrowserProfile
            {
                Name = displayName,
                ProfilePath = profileDir,
                Arguments = arguments,
                IsDefault = profileName == "Default"
            });
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