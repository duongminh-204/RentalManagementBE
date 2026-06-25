using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAdminRepository _adminRepository;

    public AuditLogService(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task LogAsync(int? userId, string action, string? entity = null, int? entityId = null, string? details = null, string? ipAddress = null)
    {
        await _adminRepository.AddAuditLogAsync(new AuditLog
        {
            UserId = userId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            IPAddress = ipAddress,
            Timestamp = DateTime.Now
        });
    }
}
