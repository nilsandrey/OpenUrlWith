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

                // Add profile-specific arguments if a profile is selected
                if (browser.SelectedProfile != null && !string.IsNullOrEmpty(browser.SelectedProfile.Arguments))
                {
                    startInfo.Arguments = $"{browser.SelectedProfile.Arguments} \"{url}\"";
                }
                else
                {
                    startInfo.Arguments = $"\"{url}\"";
                }

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
}