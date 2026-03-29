using System.IO;

namespace AppLocker.Presentation;

/// <summary>Đường dẫn dữ liệu trong %LOCALAPPDATA%\AppLocker.</summary>
public static class AppDataPaths
{
    public static string GetDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDir = Path.Combine(localAppData, "AppLocker");
        Directory.CreateDirectory(dataDir);
        return dataDir;
    }

    public static string GetDatabasePath()
    {
        return Path.Combine(GetDataDirectory(), "applocker.db");
    }
}
