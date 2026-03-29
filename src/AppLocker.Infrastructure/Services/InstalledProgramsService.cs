using Microsoft.Win32;

namespace AppLocker.Infrastructure.Services;

/// <summary>
/// Đọc danh sách phần mềm đã cài từ Registry Uninstall (HKLM / HKCU).
/// </summary>
public class InstalledProgramsService
{
    public IReadOnlyList<InstalledProgramRecord> EnumerateInstalledPrograms()
    {
        var byProcess = new Dictionary<string, InstalledProgramRecord>(StringComparer.OrdinalIgnoreCase);

        void Scan(RegistryKey root, string subPath)
        {
            using var k = root.OpenSubKey(subPath);
            if (k is null) return;

            foreach (var skName in k.GetSubKeyNames())
            {
                using var sk = k.OpenSubKey(skName);
                if (sk is null) continue;

                var disp = sk.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(disp)) continue;

                if (sk.GetValue("SystemComponent") is int sc && sc == 1)
                    continue;

                var publisher = sk.GetValue("Publisher") as string;
                var displayIcon = sk.GetValue("DisplayIcon") as string;
                var installLoc = sk.GetValue("InstallLocation") as string;

                var guess = ExtractProcessAndPath(displayIcon, installLoc);
                if (guess.ProcessName is null) continue;

                if (!byProcess.ContainsKey(guess.ProcessName))
                {
                    byProcess[guess.ProcessName] = new InstalledProgramRecord
                    {
                        DisplayName = disp.Trim(),
                        Publisher = string.IsNullOrWhiteSpace(publisher) ? null : publisher.Trim(),
                        ProcessName = guess.ProcessName,
                        ExecutablePath = guess.ExePath
                    };
                }
            }
        }

        Scan(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
        Scan(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        Scan(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

        return byProcess.Values.ToList();
    }

    private static (string? ProcessName, string? ExePath) ExtractProcessAndPath(string? displayIcon, string? installLocation)
    {
        if (!string.IsNullOrWhiteSpace(displayIcon))
        {
            var raw = displayIcon.Split(',')[0].Trim().Trim('"');
            if (raw.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                var name = Path.GetFileNameWithoutExtension(raw);
                var path = File.Exists(raw) ? raw : null;
                return (name, path ?? raw);
            }
        }

        if (string.IsNullOrWhiteSpace(installLocation))
            return (null, null);

        var dir = installLocation.Trim().Trim('"');
        if (!Directory.Exists(dir))
            return (null, null);

        try
        {
            var exes = Directory.GetFiles(dir, "*.exe", SearchOption.TopDirectoryOnly);
            if (exes.Length == 0)
                return (null, null);

            var pick = exes
                .OrderBy(p => p.Length)
                .FirstOrDefault(p => !p.Contains("unins", StringComparison.OrdinalIgnoreCase)
                                     && !p.Contains("Uninstall", StringComparison.OrdinalIgnoreCase));
            pick ??= exes[0];
            return (Path.GetFileNameWithoutExtension(pick), pick);
        }
        catch
        {
            return (null, null);
        }
    }
}

public sealed class InstalledProgramRecord
{
    public required string DisplayName { get; init; }
    public string? Publisher { get; init; }
    public required string ProcessName { get; init; }
    public string? ExecutablePath { get; init; }
}
