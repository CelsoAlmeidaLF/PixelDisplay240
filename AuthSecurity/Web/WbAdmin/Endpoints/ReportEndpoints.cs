using Systekna.Kernel.Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WbAdmin.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/reports").RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapGet("/errors/export", async (IGovernanceService gov) =>
        {
            var csv = await gov.GenerateErrorReportCsvAsync();
            return Results.File(csv, "text/csv", $"error_report_{DateTime.Now:yyyyMMdd}.csv");
        });

        group.MapGet("/audit/export", async (IAuditService audit) =>
        {
            var csv = await audit.GenerateAuditReportCsvAsync();
            return Results.File(csv, "text/csv", $"audit_report_{DateTime.Now:yyyyMMdd}.csv");
        });
    }
}
