using Backend.Data;
using Backend.Entities;
using Backend.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly RentalManagementDb _context;

    public InvoiceRepository(RentalManagementDb context)
    {
        _context = context;
    }

    public async Task<Room?> GetRoomWithDetailsAsync(int roomId)
    {
        return await _context.Rooms
            .Include(r => r.Building)
            .Include(r => r.RoomServices)
                .ThenInclude(rs => rs.Service)
            .Include(r => r.Vehicles)
            .FirstOrDefaultAsync(r => r.RoomId == roomId);
    }

    public async Task<UtilityUsage?> GetUtilityUsageAsync(int roomId, string monthYear)
    {
        return await _context.UtilityUsages
            .FirstOrDefaultAsync(u => u.RoomId == roomId && u.MonthYear == monthYear);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetAnyUserAsync()
    {
        return await _context.Users.OrderBy(u => u.Id).FirstOrDefaultAsync();
    }

    public async Task<Invoice?> GetInvoiceByRoomAndMonthAsync(int roomId, string monthYear, int? ownerUserId = null)
    {
        var query = _context.Invoices
            .Include(i => i.InvoiceDetails)
            .Include(i => i.Room)
                .ThenInclude(r => r.Contracts)
                    .ThenInclude(c => c.Tenant)
            .AsQueryable();

        return await query.FirstOrDefaultAsync(i =>
            i.RoomId == roomId &&
            i.MonthYear == monthYear &&
            (!ownerUserId.HasValue || i.Room.Building.UserId == ownerUserId.Value));
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId, int? ownerUserId = null)
    {
        var query = _context.Invoices
            .Include(i => i.InvoiceDetails)
            .Include(i => i.Room)
                .ThenInclude(r => r.Contracts)
                    .ThenInclude(c => c.Tenant)
            .AsQueryable();

        return await query.FirstOrDefaultAsync(i =>
            i.InvoiceId == invoiceId &&
            (!ownerUserId.HasValue || i.Room.Building.UserId == ownerUserId.Value));
    }

    public async Task<IEnumerable<Invoice>> SearchInvoicesAsync(int? roomId = null, string? tenantName = null, string? monthYearFrom = null, string? monthYearTo = null, string? status = null, string? search = null, int? ownerUserId = null)
    {
        var query = _context.Invoices
            .Include(i => i.InvoiceDetails)
            .Include(i => i.Room)
                .ThenInclude(r => r.Contracts)
                    .ThenInclude(c => c.Tenant)
            .AsQueryable();

        if (roomId.HasValue)
            query = query.Where(i => i.RoomId == roomId.Value);
        if (ownerUserId.HasValue)
            query = query.Where(i => i.Room.Building.UserId == ownerUserId.Value);

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            var normalizedTenantName = tenantName.Trim().ToLower();
            query = query.Where(i => i.Room.Contracts.Any(c => c.Tenant != null && c.Tenant.FullName.ToLower().Contains(normalizedTenantName)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLower();
            query = query.Where(i => i.Status.ToLower().Contains(normalizedStatus));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(i => i.InvoiceId.ToString().Contains(normalizedSearch)
                || i.Room.RoomName.ToLower().Contains(normalizedSearch)
                || i.MonthYear.ToLower().Contains(normalizedSearch)
                || i.Status.ToLower().Contains(normalizedSearch)
                || (i.Note != null && i.Note.ToLower().Contains(normalizedSearch)));
        }

        if (!string.IsNullOrWhiteSpace(monthYearFrom))
        {
            var normalizedFrom = monthYearFrom.Trim();
            query = query.Where(i => string.Compare(i.MonthYear, normalizedFrom) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(monthYearTo))
        {
            var normalizedTo = monthYearTo.Trim();
            query = query.Where(i => string.Compare(i.MonthYear, normalizedTo) <= 0);
        }

        return await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetRoomsWithDetailsAsync(int? buildingId = null, int? ownerUserId = null)
    {
        var query = _context.Rooms
            .Include(r => r.RoomServices)
                .ThenInclude(rs => rs.Service)
            .Include(r => r.Vehicles)
            .AsQueryable();

        if (buildingId.HasValue)
            query = query.Where(r => r.BuildingId == buildingId.Value);
        if (ownerUserId.HasValue)
            query = query.Where(r => r.Building.UserId == ownerUserId.Value);

        return await query.ToListAsync();
    }

    public async Task AddUtilityUsageAsync(UtilityUsage usage)
    {
        await _context.UtilityUsages.AddAsync(usage);
    }

    public Task UpdateUtilityUsageAsync(UtilityUsage usage)
    {
        _context.UtilityUsages.Update(usage);
        return Task.CompletedTask;
    }

    public async Task AddInvoiceAsync(Invoice invoice)
    {
        await _context.Invoices.AddAsync(invoice);
    }

    public Task UpdateInvoiceAsync(Invoice invoice)
    {
        _context.Invoices.Update(invoice);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteInvoiceAsync(int invoiceId, int? ownerUserId = null)
    {
        var invoice = await GetInvoiceByIdAsync(invoiceId, ownerUserId);
        if (invoice == null)
            return false;

        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
