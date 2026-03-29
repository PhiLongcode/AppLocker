using AppLocker.Domain.Interfaces;
using System.Diagnostics;

namespace AppLocker.Infrastructure.Services;

/// <summary>
/// Triển khai IEnforcementService để kill process vi phạm.
/// </summary>
public class EnforcementService : IEnforcementService
{
    public void Kill(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        foreach (var p in processes)
        {
            try
            {
                p.Kill(entireProcessTree: true);
                Console.WriteLine($"[EnforcementService] Killed: {processName} (PID: {p.Id})");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[EnforcementService] Failed to kill {processName}: {ex.Message}");
            }
        }
    }
}
