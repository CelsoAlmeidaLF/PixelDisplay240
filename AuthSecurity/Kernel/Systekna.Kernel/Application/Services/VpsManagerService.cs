using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Systekna.Kernel.Application.Services;

public class VpsManagerService : IVpsManagerService
{
    private static readonly DateTime StartTime = DateTime.UtcNow;

    public async Task<HostStats> GetHostStatsAsync()
    {
        return await Task.Run(() =>
        {
            var cpu = GetCpuUsage();
            var (memUsed, memTotal) = GetMemoryUsage();
            var (diskUsed, diskTotal) = GetDiskUsage();
            
            return new HostStats(
                CpuUsagePercentage: Math.Round(cpu, 2),
                MemoryUsedGb: Math.Round(memUsed, 2),
                MemoryTotalGb: Math.Round(memTotal, 2),
                DiskUsedGb: Math.Round(diskUsed, 2),
                DiskTotalGb: Math.Round(diskTotal, 2),
                Uptime: GetUptime(),
                OsDescription: RuntimeInformation.OSDescription,
                Status: "Running"
            );
        });
    }

    private double GetCpuUsage()
    {
#if DEBUG
        Random rnd = new();
        return 15.0 + rnd.NextDouble() * 30.0;
#else
        try
        {
            var output = ExecuteCommand("wmic cpu get loadpercentage /value");
            if (output.Contains("="))
            {
                var valStr = output.Split('=')[1].Trim();
                return double.TryParse(valStr, out var val) ? val : 0;
            }
            return 0;
        }
        catch { return 0; }
#endif
    }

    private (double used, double total) GetMemoryUsage()
    {
#if DEBUG
        return (4.2, 16.0);
#else
        try
        {
            var output = ExecuteCommand("wmic OS get FreePhysicalMemory,TotalVisibleMemorySize /Value");
            string freeStr = "", totalStr = "";
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("FreePhysicalMemory")) freeStr = line.Split('=')[1].Trim();
                if (line.StartsWith("TotalVisibleMemorySize")) totalStr = line.Split('=')[1].Trim();
            }

            if (double.TryParse(totalStr, out var totalKb) && double.TryParse(freeStr, out var freeKb))
            {
                double totalGb = totalKb / (1024.0 * 1024);
                double usedGb = (totalKb - freeKb) / (1024.0 * 1024);
                return (usedGb, totalGb);
            }
            return (0, 0);
        }
        catch { return (0, 0); }
#endif
    }

    private (double used, double total) GetDiskUsage()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady);
            if (drive == null) return (0, 0);

            double total = drive.TotalSize / (1024.0 * 1024 * 1024);
            double free = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
            return (total - free, total);
        }
        catch { return (0, 0); }
    }

    private string GetUptime()
    {
        var uptime = DateTime.UtcNow - StartTime;
        return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
    }

    private string ExecuteCommand(string command)
    {
        try
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result.Trim();
        }
        catch { return ""; }
    }

    public async Task<List<string>> GetSystemLogsAsync(int lines = 50)
    {
#if DEBUG
        return await Task.FromResult(new List<string>
        {
            $"[{DateTime.Now:T}] DEBUG: Heartbeat OK",
            $"[{DateTime.Now:T}] DEBUG: Monitoring simulated events"
        });
#else
        try
        {
            // Windows Event Log via PowerShell
            var output = ExecuteCommandPowerShell($"Get-EventLog -LogName System -Newest {lines} | Select-Object -Property TimeGenerated, EntryType, Message | Format-Table -HideTableHeaders");
            return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        catch { return new List<string> { "Não foi possível carregar os logs do Windows." }; }
#endif
    }

    private string ExecuteCommandPowerShell(string command)
    {
        try
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result.Trim();
        }
        catch { return ""; }
    }

    public async Task<bool> RestartServiceAsync(string serviceName)
    {
#if DEBUG
        Console.WriteLine($">>> VPS MANAGER: Restarting service {serviceName}...");
        return await Task.FromResult(true);
#else
        try
        {
            await Task.Run(() =>
            {
                Process.Start("cmd.exe", $"/c \"sc stop {serviceName} && sc start {serviceName}\"");
            });
            return true;
        }
        catch { return false; }
#endif
    }
}
