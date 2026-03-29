using AppLocker.Domain.Entities;
using System.Text.Json;

namespace AppLocker.Infrastructure.Storage;

/// <summary>
/// Đọc/ghi cấu hình AppRule từ file JSON.
/// </summary>
public class JsonStorageService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public JsonStorageService(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>Đọc danh sách rule từ file JSON. Trả về list rỗng nếu file chưa tồn tại.</summary>
    public List<AppRule> LoadRules()
    {
        if (!File.Exists(_filePath))
            return new List<AppRule>();

        try
        {
            var json = File.ReadAllText(_filePath);
            var config = JsonSerializer.Deserialize<AppLockConfig>(json, _options);
            return config?.Rules ?? new List<AppRule>();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[StorageService] Failed to load config: {ex.Message}");
            return new List<AppRule>();
        }
    }

    /// <summary>Lưu danh sách rule ra file JSON.</summary>
    public void SaveRules(IEnumerable<AppRule> rules)
    {
        var config = new AppLockConfig { Rules = rules.ToList() };
        var json = JsonSerializer.Serialize(config, _options);

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        File.WriteAllText(_filePath, json);
    }

    private class AppLockConfig
    {
        public List<AppRule> Rules { get; set; } = new();
    }
}
