using AppLocker.Domain.Entities;
using AppLocker.Domain.Interfaces;
using Microsoft.Data.Sqlite;

namespace AppLocker.Infrastructure.Storage;

/// <summary>
/// SQLite storage service - lưu AppRule và UsageHistory thay thế JSON ở Phase 3.
/// </summary>
public class SqliteStorageService : IDisposable, IMasterPasswordStore
{
    private readonly SqliteConnection _connection;

    public SqliteStorageService(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS AppRules (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessName TEXT    NOT NULL UNIQUE,
                RuleType    TEXT    NOT NULL,
                TimeLimitMinutes INTEGER,
                IsEnabled   INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS UsageHistory (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessName TEXT    NOT NULL,
                Date        TEXT    NOT NULL,
                TotalMinutes REAL   NOT NULL DEFAULT 0,
                UNIQUE(ProcessName, Date)
            );

            CREATE TABLE IF NOT EXISTS AppSettings (
                Key   TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private const string MasterPasswordKey = "master_password_hash";

    public string? LoadMasterPasswordHash()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM AppSettings WHERE Key = $k";
        cmd.Parameters.AddWithValue("$k", MasterPasswordKey);
        var scalar = cmd.ExecuteScalar();
        return scalar is string s && s.Length > 0 ? s : null;
    }

    public void SaveMasterPasswordHash(string sha256HexLower)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AppSettings (Key, Value) VALUES ($k, $v)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        cmd.Parameters.AddWithValue("$k", MasterPasswordKey);
        cmd.Parameters.AddWithValue("$v", sha256HexLower);
        cmd.ExecuteNonQuery();
    }

    // ── AppRule CRUD ──────────────────────────────────────────────────────────

    public List<AppRule> GetAllRules()
    {
        var rules = new List<AppRule>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT ProcessName, RuleType, TimeLimitMinutes, IsEnabled FROM AppRules";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            rules.Add(new AppRule
            {
                ProcessName = reader.GetString(0),
                Type = Enum.Parse<RuleType>(reader.GetString(1)),
                TimeLimitMinutes = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                IsEnabled = reader.GetInt32(3) == 1
            });
        }
        return rules;
    }

    /// <summary>Đọc rule — tương thích API với lưu trữ JSON cũ.</summary>
    public List<AppRule> LoadRules() => GetAllRules();

    /// <summary>Thay thế toàn bộ rule trong DB (đồng bộ với UI Settings).</summary>
    public void SaveRules(IEnumerable<AppRule> rules)
    {
        using var tx = _connection.BeginTransaction();
        using (var del = _connection.CreateCommand())
        {
            del.Transaction = tx;
            del.CommandText = "DELETE FROM AppRules";
            del.ExecuteNonQuery();
        }

        foreach (var rule in rules)
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO AppRules (ProcessName, RuleType, TimeLimitMinutes, IsEnabled)
                VALUES ($name, $type, $limit, $enabled);
                """;
            cmd.Parameters.AddWithValue("$name", rule.ProcessName);
            cmd.Parameters.AddWithValue("$type", rule.Type.ToString());
            cmd.Parameters.AddWithValue("$limit", rule.TimeLimitMinutes.HasValue ? rule.TimeLimitMinutes.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("$enabled", rule.IsEnabled ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public void UpsertRule(AppRule rule)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO AppRules (ProcessName, RuleType, TimeLimitMinutes, IsEnabled)
            VALUES ($name, $type, $limit, $enabled)
            ON CONFLICT(ProcessName) DO UPDATE SET
                RuleType = excluded.RuleType,
                TimeLimitMinutes = excluded.TimeLimitMinutes,
                IsEnabled = excluded.IsEnabled;
            """;
        cmd.Parameters.AddWithValue("$name", rule.ProcessName);
        cmd.Parameters.AddWithValue("$type", rule.Type.ToString());
        cmd.Parameters.AddWithValue("$limit", rule.TimeLimitMinutes.HasValue ? rule.TimeLimitMinutes.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("$enabled", rule.IsEnabled ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void DeleteRule(string processName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM AppRules WHERE ProcessName = $name COLLATE NOCASE";
        cmd.Parameters.AddWithValue("$name", processName);
        cmd.ExecuteNonQuery();
    }

    // ── Usage History ─────────────────────────────────────────────────────────

    public void AddUsageMinutes(string processName, double minutes)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO UsageHistory (ProcessName, Date, TotalMinutes)
            VALUES ($name, $date, $mins)
            ON CONFLICT(ProcessName, Date) DO UPDATE SET
                TotalMinutes = TotalMinutes + excluded.TotalMinutes;
            """;
        cmd.Parameters.AddWithValue("$name", processName);
        cmd.Parameters.AddWithValue("$date", today);
        cmd.Parameters.AddWithValue("$mins", minutes);
        cmd.ExecuteNonQuery();
    }

    public double GetUsageMinutesToday(string processName)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT TotalMinutes FROM UsageHistory WHERE ProcessName = $name COLLATE NOCASE AND Date = $date";
        cmd.Parameters.AddWithValue("$name", processName);
        cmd.Parameters.AddWithValue("$date", today);
        var val = cmd.ExecuteScalar();
        return val is null ? 0 : Convert.ToDouble(val);
    }

    public void Dispose()
    {
        SqliteConnection.ClearPool(_connection);
        _connection.Dispose();
    }
}
