using System.IO;

namespace AppLocker.Presentation;

/// <summary>Đường dẫn dữ liệu cạnh exe (Data/applocker.db).</summary>
public static class AppDataPaths
{
    public static string GetDatabasePath()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        return Path.Combine(dataDir, "applocker.db");
    }
}
