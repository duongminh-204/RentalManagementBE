using Backend.DTOs.Vehicles;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicles;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "active", "inactive", "unknown"
    };

    public VehicleService(IVehicleRepository vehicles, IWebHostEnvironment env)
    {
        _vehicles = vehicles;
        _env = env;
    }

    public async Task<IEnumerable<VehicleDto>> GetAllAsync(string? status = null, string? type = null, string? search = null)
    {
        var query = _vehicles.QueryWithTenantAndRoom();

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            var s = status.Trim().ToLowerInvariant();
            if (s == "unknown")
                query = query.Where(v => v.Status == "unknown" || v.TenantId == null);
            else
                query = query.Where(v => v.Status == s);
        }

        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "all", StringComparison.OrdinalIgnoreCase))
        {
            var t = type.Trim().ToLowerInvariant();
            query = query.Where(v => v.VehicleType != null && v.VehicleType.ToLower() == t);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            query = query.Where(v =>
                v.LicensePlateNumber.ToLower().Contains(q) ||
                (v.Brand != null && v.Brand.ToLower().Contains(q)) ||
                (v.Tenant != null && v.Tenant.FullName.ToLower().Contains(q)));
        }

        var list = await query.OrderByDescending(v => v.UpdatedAt).ToListAsync();
        return list.Select(MapToDto);
    }

    public async Task<VehicleDto?> GetByIdAsync(int id)
    {
        var vehicle = await _vehicles.QueryWithTenantAndRoom().FirstOrDefaultAsync(v => v.VehicleId == id);
        return vehicle == null ? null : MapToDto(vehicle);
    }

    public async Task<IEnumerable<VehicleDto>> SearchByLicensePlateAsync(string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
            return Array.Empty<VehicleDto>();

        var q = licensePlate.Trim().ToLowerInvariant();
        var list = await _vehicles.QueryWithTenantAndRoom()
            .Where(v => v.LicensePlateNumber.ToLower().Contains(q))
            .ToListAsync();
        return list.Select(MapToDto);
    }

    public async Task<IEnumerable<VehicleDto>> GetByRoomIdAsync(int roomId)
    {
        var list = await _vehicles.QueryWithTenantAndRoom().Where(v => v.RoomId == roomId).ToListAsync();
        return list.Select(MapToDto);
    }

    public async Task<IEnumerable<VehicleDto>> GetByTenantIdAsync(int tenantId)
    {
        var list = await _vehicles.QueryWithTenantAndRoom().Where(v => v.TenantId == tenantId).ToListAsync();
        return list.Select(MapToDto);
    }

    public async Task<IEnumerable<VehicleDto>> GetUnknownAsync()
    {
        var list = await _vehicles.QueryWithTenantAndRoom()
            .Where(v => v.Status == "unknown" || v.TenantId == null)
            .ToListAsync();
        return list.Select(MapToDto);
    }

    public async Task<ParkingFeeSummaryDto> GetParkingFeeSummaryAsync()
    {
        var active = await _vehicles.ListActiveForParkingSummaryAsync();
        return new ParkingFeeSummaryDto
        {
            TotalMonthlyFee = active.Sum(v => v.ParkingFee),
            ActiveVehicleCount = active.Count
        };
    }

    public async Task<IEnumerable<VehicleDto>> GetByTypeAsync(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return Array.Empty<VehicleDto>();

        var t = type.Trim().ToLowerInvariant();
        var list = await _vehicles.QueryWithTenantAndRoom()
            .Where(v => v.VehicleType != null && v.VehicleType.ToLower() == t)
            .ToListAsync();
        return list.Select(MapToDto);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto)
    {
        await ValidateDtoAsync(dto, excludeVehicleId: null);

        var vehicle = MapFromDto(new Vehicle(), dto);
        vehicle.CreatedAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;

        _vehicles.Add(vehicle);
        await _vehicles.SaveChangesAsync();

        return (await GetByIdAsync(vehicle.VehicleId))!;
    }

    public async Task<VehicleDto> UpdateAsync(int id, UpdateVehicleDto dto)
    {
        var vehicle = await _vehicles.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy xe.");

        await ValidateDtoAsync(dto, excludeVehicleId: id);

        MapFromDto(vehicle, dto);
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _vehicles.SaveChangesAsync();

        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var vehicle = await _vehicles.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy xe.");

        if (!string.IsNullOrEmpty(vehicle.VehicleImage))
            TryDeletePhysicalFile(vehicle.VehicleImage);

        _vehicles.Remove(vehicle);
        await _vehicles.SaveChangesAsync();
    }

    public async Task<string> UploadImageAsync(int id, IFormFile file)
    {
        var vehicle = await _vehicles.GetTrackedByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy xe.");

        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File không hợp lệ.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh tối đa 5MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG, WEBP.");

        var uploadsDir = Path.Combine(
            _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
            "uploads",
            "vehicles");
        Directory.CreateDirectory(uploadsDir);

        if (!string.IsNullOrEmpty(vehicle.VehicleImage))
            TryDeletePhysicalFile(vehicle.VehicleImage);

        var fileName = $"{id}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        vehicle.VehicleImage = $"/uploads/vehicles/{fileName}";
        vehicle.UpdatedAt = DateTime.UtcNow;
        await _vehicles.SaveChangesAsync();

        return vehicle.VehicleImage;
    }

    private async Task ValidateDtoAsync(CreateVehicleDto dto, int? excludeVehicleId)
    {
        var plate = NormalizePlate(dto.LicensePlate);
        if (string.IsNullOrEmpty(plate))
            throw new InvalidOperationException("Biển số xe không được để trống.");

        var status = NormalizeStatus(dto.Status);
        dto.Status = status;

        if (await _vehicles.LicensePlateExistsAsync(plate, excludeVehicleId))
            throw new InvalidOperationException("Biển số xe đã tồn tại.");

        if (status == "active")
        {
            if (!dto.TenantId.HasValue)
                throw new InvalidOperationException("Xe đang gửi phải gán khách thuê.");
            if (!dto.RoomId.HasValue)
                throw new InvalidOperationException("Xe đang gửi phải gán phòng.");
        }

        if (dto.TenantId.HasValue)
            await EnsureTenantExistsAsync(dto.TenantId.Value);

        if (dto.RoomId.HasValue && !await _vehicles.RoomExistsAsync(dto.RoomId.Value))
            throw new InvalidOperationException("Không tìm thấy phòng.");
    }

    private async Task EnsureTenantExistsAsync(int tenantId)
    {
        if (!await _vehicles.TenantExistsAsync(tenantId))
            throw new InvalidOperationException("Không tìm thấy khách thuê.");
    }

    private static Vehicle MapFromDto(Vehicle vehicle, CreateVehicleDto dto)
    {
        var status = NormalizeStatus(dto.Status);
        vehicle.LicensePlateNumber = NormalizePlate(dto.LicensePlate);
        vehicle.VehicleType = dto.Type?.Trim();
        vehicle.Brand = dto.Brand?.Trim();
        vehicle.Color = dto.Color?.Trim();
        vehicle.ParkingFee = dto.ParkingFee;
        vehicle.Status = status;
        vehicle.Notes = dto.Notes?.Trim();
        vehicle.RegistrationDate = dto.RegistrationDate?.Date;
        vehicle.TenantId = dto.TenantId;
        vehicle.RoomId = dto.RoomId;
        return vehicle;
    }

    private static string NormalizeStatus(string? status)
    {
        var s = string.IsNullOrWhiteSpace(status) ? "active" : status.Trim().ToLowerInvariant();
        if (!ValidStatuses.Contains(s))
            return "active";
        return s;
    }

    private static string NormalizePlate(string? plate) =>
        string.IsNullOrWhiteSpace(plate) ? string.Empty : plate.Trim().ToUpperInvariant();

    private static VehicleDto MapToDto(Vehicle v) => new()
    {
        Id = v.VehicleId,
        LicensePlate = v.LicensePlateNumber,
        Type = v.VehicleType,
        Brand = v.Brand,
        Color = v.Color,
        ImageUrl = v.VehicleImage,
        ParkingFee = v.ParkingFee,
        Status = string.IsNullOrEmpty(v.Status)
            ? (v.TenantId == null ? "unknown" : "active")
            : v.Status,
        Notes = v.Notes,
        RegistrationDate = v.RegistrationDate ?? v.CreatedAt,
        TenantId = v.TenantId,
        TenantName = v.Tenant?.FullName,
        RoomId = v.RoomId,
        RoomNumber = v.Room?.RoomName,
        CreatedAt = v.CreatedAt,
        UpdatedAt = v.UpdatedAt
    };

    private void TryDeletePhysicalFile(string relativePath)
    {
        try
        {
            var fileName = Path.GetFileName(relativePath);
            if (string.IsNullOrEmpty(fileName)) return;

            var fullPath = Path.Combine(
                _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
                "uploads",
                "vehicles",
                fileName);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // ignore cleanup errors
        }
    }
}
