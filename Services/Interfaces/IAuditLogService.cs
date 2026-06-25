using Backend.Entities;

namespace Backend.Services.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(int? userId, string action, string? entity = null, int? entityId = null, string? details = null, string? ipAddress = null);
}
