using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace OpenWithTool.Services;

public interface IUrlProtocolRegistrationService
{
    bool IsRegisteredAsDefaultBrowser();
    bool RegisterAsDefaultBrowser();
    bool UnregisterAsDefaultBrowser();
    bool IsRunningAsAdministrator();
    Task<bool> RegisterElevatedAsync();
    Task<bool> UnregisterElevatedAsync();
}

public class UrlProtocolRegistrationService : IUrlProtocolRegistrationService
{
    private const string AppName = "OpenWithTool";
    private const string AppDescription = "OpenWith Tool - Browser Selector";

    public bool IsRegisteredAsDefaultBrowser()
    {
        try
        {
            // Check if our app is registered in the system
            using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Clients\StartMenuInternet\{AppName}");
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    public bool RegisterAsDefaultBrowser()
    {
        try
        {
            if (!IsRunningAsAdministrator())
                return false;

            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return false;

            // Register as a browser
            RegisterBrowser(exePath);
            
            // Register URL protocols
            RegisterUrlProtocol("http", exePath);
            RegisterUrlProtocol("https", exePath);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register as default browser: {ex.Message}");
            return false;
        }
    }

    public bool UnregisterAsDefaultBrowser()
    {
        try
        {
            if (!IsRunningAsAdministrator())
                return false;

            Registry.LocalMachine.DeleteSubKeyTree($@"SOFTWARE\Clients\StartMenuInternet\{AppName}", false);
            using (var registeredApplicationsKey = Registry.LocalMachine.OpenSubKey(
                       @"SOFTWARE\RegisteredApplications",
                       writable: true))
            {
                registeredApplicationsKey?.DeleteValue(AppName, throwOnMissingValue: false);
            }

            Registry.ClassesRoot.DeleteSubKeyTree($"{AppName}URL", throwOnMissingSubKey: false);
            Registry.ClassesRoot.DeleteSubKeyTree($"{AppName}HTML", throwOnMissingSubKey: false);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to unregister as default browser: {ex.Message}");
            return false;
        }
    }

    public bool IsRunningAsAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RegisterElevatedAsync()
    {
        if (IsRunningAsAdministrator())
            return RegisterAsDefaultBrowser();

        return await RunElevatedCommandAsync("--register") && IsRegisteredAsDefaultBrowser();
    }

    public async Task<bool> UnregisterElevatedAsync()
    {
        if (IsRunningAsAdministrator())
            return UnregisterAsDefaultBrowser();

        return await RunElevatedCommandAsync("--unregister") && !IsRegisteredAsDefaultBrowser();
    }

    private static async Task<bool> RunElevatedCommandAsync(string command)
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
                return false;

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = command,
                UseShellExecute = true,
                Verb = "runas"
            });

            if (process == null)
                return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void RegisterBrowser(string exePath)
    {
        // Register in StartMenuInternet
        using var browserKey = Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Clients\StartMenuInternet\{AppName}");
        browserKey.SetValue("", AppDescription);

        // Default icon
        using var defaultIconKey = browserKey.CreateSubKey("DefaultIcon");
        defaultIconKey.SetValue("", $"\"{exePath}\",0");

        // Install info
        using var installInfoKey = browserKey.CreateSubKey("InstallInfo");
        installInfoKey.SetValue("ReinstallCommand", $"\"{exePath}\" --register");
        installInfoKey.SetValue("HideIconsCommand", $"\"{exePath}\" --hide-icons");
        installInfoKey.SetValue("ShowIconsCommand", $"\"{exePath}\" --show-icons");
        installInfoKey.SetValue("IconsVisible", 1, RegistryValueKind.DWord);

        // Shell open command
        using var shellKey = browserKey.CreateSubKey(@"shell\open\command");
        shellKey.SetValue("", $"\"{exePath}\" \"%1\"");

        // Capabilities
        using var capabilitiesKey = browserKey.CreateSubKey("Capabilities");
        capabilitiesKey.SetValue("ApplicationName", AppDescription);
        capabilitiesKey.SetValue("ApplicationDescription", "Browser selection tool for opening web links");
        capabilitiesKey.SetValue("ApplicationIcon", $"{exePath},0");

        // File associations
        using var fileAssocKey = capabilitiesKey.CreateSubKey("FileAssociations");
        fileAssocKey.SetValue(".htm", $"{AppName}HTML");
        fileAssocKey.SetValue(".html", $"{AppName}HTML");

        // URL associations
        using var urlAssocKey = capabilitiesKey.CreateSubKey("URLAssociations");
        urlAssocKey.SetValue("http", $"{AppName}URL");
        urlAssocKey.SetValue("https", $"{AppName}URL");

        // Register in RegisteredApplications
        using var regAppsKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\RegisteredApplications");
        regAppsKey.SetValue(AppName, $@"SOFTWARE\Clients\StartMenuInternet\{AppName}\Capabilities");
    }

    private void RegisterUrlProtocol(string protocol, string exePath)
    {
        // Register URL protocol handler
        using var protocolKey = Registry.ClassesRoot.CreateSubKey($"{AppName}URL");
        protocolKey.SetValue("", $"URL:{protocol} Protocol");
        protocolKey.SetValue("URL Protocol", "");

        using var defaultIconKey = protocolKey.CreateSubKey("DefaultIcon");
        defaultIconKey.SetValue("", $"\"{exePath}\",0");

        using var shellKey = protocolKey.CreateSubKey(@"shell\open\command");
        shellKey.SetValue("", $"\"{exePath}\" \"%1\"");

        // Register HTML file type
        using var htmlKey = Registry.ClassesRoot.CreateSubKey($"{AppName}HTML");
        htmlKey.SetValue("", "HTML Document");

        using var htmlDefaultIconKey = htmlKey.CreateSubKey("DefaultIcon");
        htmlDefaultIconKey.SetValue("", $"\"{exePath}\",0");

        using var htmlShellKey = htmlKey.CreateSubKey(@"shell\open\command");
        htmlShellKey.SetValue("", $"\"{exePath}\" \"%1\"");
    }
}
