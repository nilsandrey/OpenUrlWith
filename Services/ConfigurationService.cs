using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;
using OpenWithTool.Models;

namespace OpenWithTool.Services;

public interface IConfigurationService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    Task<string> GetLastSelectedBrowserAsync();
    Task SaveLastSelectedBrowserAsync(string browserName, string profileName);
}

public class ConfigurationService : IConfigurationService
{
    private readonly string _settingsPath;
    private AppSettings? _cachedSettings;

    public ConfigurationService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "OpenWithTool");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        if (!File.Exists(_settingsPath))
        {
            _cachedSettings = new AppSettings();
            await SaveSettingsAsync(_cachedSettings);
            return _cachedSettings;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath);
            _cachedSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            _cachedSettings = new AppSettings();
        }

        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _cachedSettings = settings;
        try
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    public async Task<string> GetLastSelectedBrowserAsync()
    {
        var settings = await GetSettingsAsync();
        return settings.LastSelectedBrowser;
    }

    public async Task SaveLastSelectedBrowserAsync(string browserName, string profileName)
    {
        var settings = await GetSettingsAsync();
        settings.LastSelectedBrowser = browserName;
        settings.LastSelectedProfile = profileName;
        await SaveSettingsAsync(settings);
    }
}