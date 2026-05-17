using Backend.Data;
using Backend.DTOs.Tenants;
using Backend.Entities;
using Backend.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class TenantService : ITenantService
{
    private readonly RentalManagementDb _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IWebHostEnvironment _env;

    public TenantService(
        RentalManagementDb context,
        IPasswordHasher<User> passwordHasher,
        IWebHostEnvironment env)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _env = env;
    }

    public async Task<IEnumerable<TenantListDto>> GetAllAsync(string? status = null, string? search = null)
    {
        var tenantRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Tenant");
        if (tenantRole == null)
            return Array.Empty<TenantListDto>();

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.RoleId == tenantRole.RoleId)
            .Include(u => u.Contracts)
            .ThenInclude(c => c.Room)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(q) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(q)) ||
                (u.CCCD != null && u.CCCD.Contains(q)) ||
                (u.Email != null && u.Email.ToLower().Contains(q)));
        }

        var users = await query.OrderByDescending(u => u.UpdatedAt).ToListAsync();
        var list = users.Select(MapToListDto).ToList();

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            list = list.Where(t => t.Status == status).ToList();
        }

        return list;
    }

    public async Task<TenantDetailDto?> GetByIdAsync(int id)
    {
        var user = await GetTenantUserQuery().FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return null;

        var dto = MapToDetailDto(user);
        dto.History = await GetHistoryInternalAsync(id);
        return dto;
    }

    public async Task<TenantDetailDto> CreateAsync(CreateTenantDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("Họ tên không được để trống.");

        var tenantRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Tenant")
            ?? throw new InvalidOperationException("Không tìm thấy vai trò Tenant.");

        var email = ResolveEmail(dto.Email, dto.PhoneNumber);
        var phone = dto.PhoneNumber?.Trim();

        if (await _context.Users.AnyAsync(u =>
                (email != null && u.Email == email) ||
                (phone != null && u.PhoneNumber == phone)))
        {
            throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng.");
        }

        var password = string.IsNullOrWhiteSpace(dto.Password) ? "Tenant@123" : dto.Password!;
        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            PhoneNumber = phone,
            CCCD = dto.Cccd?.Trim(),
            Address = dto.Address?.Trim(),
            RoleId = tenantRole.RoleId,
            IsActive = true,
            PasswordHash = _passwordHasher.HashPassword(new User(), password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (dto.RoomId.HasValue)
        {
            await CreateOrUpdateActiveContractAsync(user.UserId, dto.RoomId.Value, dto);
        }

        return (await GetByIdAsync(user.UserId))!;
    }

    public async Task<TenantDetailDto> UpdateAsync(int id, UpdateTenantDto dto)
    {
        var user = await _context.Users
            .Include(u => u.Contracts)
            .FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            throw new InvalidOperationException("Họ tên không được để trống.");

        var email = ResolveEmail(dto.Email, dto.PhoneNumber);
        var phone = dto.PhoneNumber?.Trim();

        if (await _context.Users.AnyAsync(u =>
                u.UserId != id &&
                ((email != null && u.Email == email) || (phone != null && u.PhoneNumber == phone))))
        {
            throw new InvalidOperationException("Email hoặc số điện thoại đã được sử dụng.");
        }

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.PhoneNumber = phone;
        user.CCCD = dto.Cccd?.Trim();
        user.Address = dto.Address?.Trim();
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (dto.RoomId.HasValue)
        {
            await CreateOrUpdateActiveContractAsync(id, dto.RoomId.Value, dto);
        }
        else if (dto.Status == "moved_out")
        {
            var active = user.Contracts.FirstOrDefault(c => c.Status == "Active");
            if (active != null)
            {
                active.Status = "Terminated";
                active.EndDate = DateTime.UtcNow.Date;
                active.Note = dto.Notes ?? active.Note;
            }
        }

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _context.Users
            .Include(u => u.Contracts)
            .FirstOrDefaultAsync(u => u.UserId == id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        foreach (var c in user.Contracts.Where(c => c.Status == "Active"))
        {
            c.Status = "Terminated";
            c.EndDate = DateTime.UtcNow.Date;
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<string> UploadIdCardAsync(int id, IFormFile file)
    {
        var user = await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy khách thuê.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh tối đa 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG, WEBP.");

        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "cccd");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        user.CCCDImage = $"/uploads/cccd/{fileName}";
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user.CCCDImage;
    }

    public async Task<IEnumerable<TenantHistoryDto>> GetHistoryAsync(int id)
    {
        await EnsureTenantExists(id);
        return await GetHistoryInternalAsync(id);
    }

    private async Task CreateOrUpdateActiveContractAsync(int userId, int roomId, CreateTenantDto dto)
    {
        var room = await _context.Rooms.FindAsync(roomId)
            ?? throw new KeyNotFoundException("Không tìm thấy phòng.");

        var existing = await _context.Contracts
            .FirstOrDefaultAsync(c => c.UserId == userId && c.RoomId == roomId && c.Status == "Active");

        var start = dto.MoveInDate?.Date ?? DateTime.UtcNow.Date;
        var end = dto.MoveOutDate?.Date ?? start.AddYears(1);

        if (existing != null)
        {
            existing.StartDate = start;
            existing.EndDate = end;
            existing.Deposit = dto.Deposit;
            existing.Note = dto.Notes;
        }
        else
        {
            var otherActive = await _context.Contracts
                .Where(c => c.UserId == userId && c.Status == "Active")
                .ToListAsync();
            foreach (var c in otherActive)
            {
                c.Status = "Terminated";
                c.EndDate = DateTime.UtcNow.Date;
            }

            _context.Contracts.Add(new Contract
            {
                UserId = userId,
                RoomId = roomId,
                StartDate = start,
                EndDate = end,
                Deposit = dto.Deposit,
                Note = dto.Notes,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!string.Equals(room.Status, "Occupied", StringComparison.OrdinalIgnoreCase))
            room.Status = "Occupied";
    }

    private IQueryable<User> GetTenantUserQuery() =>
        _context.Users
            .Include(u => u.Contracts)
            .ThenInclude(c => c.Room);

    private async Task EnsureTenantExists(int id)
    {
        if (!await _context.Users.AnyAsync(u => u.UserId == id))
            throw new KeyNotFoundException("Không tìm thấy khách thuê.");
    }

    private async Task<List<TenantHistoryDto>> GetHistoryInternalAsync(int userId) =>
        await _context.Contracts
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Include(c => c.Room)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new TenantHistoryDto
            {
                ContractId = c.ContractId,
                RoomId = c.RoomId,
                RoomNumber = c.Room.RoomName,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Deposit = c.Deposit,
                Status = c.Status,
                Notes = c.Note,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

    private static string? ResolveEmail(string? email, string? phone)
    {
        if (!string.IsNullOrWhiteSpace(email)) return email.Trim();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var digits = new string(phone.Where(char.IsDigit).ToArray());
            return $"tenant_{digits}@rental.local";
        }
        return null;
    }

    private static TenantListDto MapToListDto(User user)
    {
        var active = user.Contracts
            .Where(c => c.Status == "Active")
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefault();
        var latest = user.Contracts.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

        return new TenantListDto
        {
            Id = user.UserId,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            Cccd = user.CCCD,
            IdCardImage = user.CCCDImage,
            Avatar = user.Avatar,
            Address = user.Address,
            IsActive = user.IsActive,
            Status = MapStatus(user, active, latest),
            RoomId = active?.RoomId,
            RoomNumber = active?.Room?.RoomName,
            ContractId = active?.ContractId,
            MoveInDate = active?.StartDate,
            MoveOutDate = active?.Status == "Terminated" ? active.EndDate : null,
            Deposit = active?.Deposit ?? 0,
            Notes = active?.Note,
            CreatedAt = user.CreatedAt
        };
    }

    private static TenantDetailDto MapToDetailDto(User user)
    {
        var list = MapToListDto(user);
        return new TenantDetailDto
        {
            Id = list.Id,
            FullName = list.FullName,
            PhoneNumber = list.PhoneNumber,
            Email = list.Email,
            Cccd = list.Cccd,
            IdCardImage = list.IdCardImage,
            Avatar = list.Avatar,
            Address = list.Address,
            IsActive = list.IsActive,
            Status = list.Status,
            RoomId = list.RoomId,
            RoomNumber = list.RoomNumber,
            ContractId = list.ContractId,
            MoveInDate = list.MoveInDate,
            MoveOutDate = list.MoveOutDate,
            Deposit = list.Deposit,
            Notes = list.Notes,
            CreatedAt = list.CreatedAt
        };
    }

    private static string MapStatus(User user, Contract? active, Contract? latest)
    {
        if (active != null) return "active";
        if (latest != null && latest.Status == "Terminated") return "moved_out";
        if (!user.IsActive) return "inactive";
        return "inactive";
    }
}
