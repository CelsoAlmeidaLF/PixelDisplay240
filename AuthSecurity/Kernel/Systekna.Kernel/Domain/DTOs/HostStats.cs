namespace Systekna.Kernel.Domain.DTOs;

public record HostStats(
    double CpuUsagePercentage,
    double MemoryUsedGb,
    double MemoryTotalGb,
    double DiskUsedGb,
    double DiskTotalGb,
    string Uptime,
    string OsDescription,
    string Status
);
