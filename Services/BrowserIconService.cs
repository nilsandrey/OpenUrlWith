using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;

namespace OpenWithTool.Services;

public interface IBrowserIconService
{
    Task<string?> GetIconPathAsync(string executablePath);
}

public sealed class BrowserIconService : IBrowserIconService
{
    private readonly string _iconCacheDirectory;

    public BrowserIconService()
    {
        _iconCacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenWithTool",
            "icons");
        Directory.CreateDirectory(_iconCacheDirectory);
    }

    public Task<string?> GetIconPathAsync(string executablePath)
    {
        return Task.Run(() => ExtractIcon(executablePath));
    }

    private string? ExtractIcon(string executablePath)
    {
        if (!File.Exists(executablePath))
            return null;

        try
        {
            var cacheKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(executablePath.ToUpperInvariant())));
            var outputPath = Path.Combine(_iconCacheDirectory, $"{cacheKey}.png");
            var executableTimestamp = File.GetLastWriteTimeUtc(executablePath);

            if (File.Exists(outputPath) && File.GetLastWriteTimeUtc(outputPath) >= executableTimestamp)
                return outputPath;

            using var icon = Icon.ExtractAssociatedIcon(executablePath);
            if (icon == null)
                return null;

            using var bitmap = icon.ToBitmap();
            bitmap.Save(outputPath, ImageFormat.Png);
            File.SetLastWriteTimeUtc(outputPath, executableTimestamp);
            return outputPath;
        }
        catch
        {
            return null;
        }
    }
}
