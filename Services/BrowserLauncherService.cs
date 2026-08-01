using System;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenWithTool.Models;

namespace OpenWithTool.Services;

public interface IBrowserLauncherService
{
    Task<bool> LaunchBrowserAsync(BrowserInfo browser, string url);
}

public class BrowserLauncherService : IBrowserLauncherService
{
    public async Task<bool> LaunchBrowserAsync(BrowserInfo browser, string url)
    {
        return await Task.Run(() =>
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = browser.ExecutablePath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                AddProfileArguments(startInfo, browser);
                startInfo.ArgumentList.Add(url);

                var process = Process.Start(startInfo);
                return process != null;
            }
            catch (Exception ex)
            {
                // Log the exception if needed
                System.Diagnostics.Debug.WriteLine($"Failed to launch browser: {ex.Message}");
                return false;
            }
        });
    }

    private static void AddProfileArguments(ProcessStartInfo startInfo, BrowserInfo browser)
    {
        var profile = browser.SelectedProfile;
        if (profile == null || string.IsNullOrWhiteSpace(profile.ProfilePath))
            return;

        if (browser.Name.Equals("firefox", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("-profile");
            startInfo.ArgumentList.Add(profile.ProfilePath);
            return;
        }

        if (browser.Name.Equals("chrome", StringComparison.OrdinalIgnoreCase)
            || browser.Name.Equals("edge", StringComparison.OrdinalIgnoreCase)
            || browser.Name.Equals("brave", StringComparison.OrdinalIgnoreCase)
            || browser.Name.Equals("vivaldi", StringComparison.OrdinalIgnoreCase))
        {
            var profileDirectoryName = Path.GetFileName(profile.ProfilePath);
            if (!string.IsNullOrWhiteSpace(profileDirectoryName))
                startInfo.ArgumentList.Add($"--profile-directory={profileDirectoryName}");
        }
    }
}
