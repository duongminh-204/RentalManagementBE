using Backend.Data;
using Backend.DTOs.Dashboard;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly RentalManagementDb _context;

    public DashboardRepository(RentalManagementDb context)
    {
        _context = context;
    }

    public async Task<List<DashboardRoomStatusRecordDto>> GetRoomStatusesAsync(int? buildingId = null)
    {
        var roomQuery = _context.Rooms.AsNoTracking().AsQueryable();
        if (buildingId.HasValue)
        {
            roomQuery = roomQuery.Where(room => room.BuildingId == buildingId.Value);
        }

        return await roomQuery
            .Select(room => new DashboardRoomStatusRecordDto
            {
                Status = room.Status,
            })
            .ToListAsync();
    }

    public async Task<List<DashboardDebtRecordDto>> GetDebtRecordsAsync(int? buildingId = null)
    {
        var invoiceQuery = _context.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Payments)
            .Include(invoice => invoice.Room)
                .ThenInclude(room => room.Contracts)
                    .ThenInclude(contract => contract.Tenant)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(invoice => invoice.Room.BuildingId == buildingId.Value);
        }

        return await invoiceQuery
            .Select(invoice => new DashboardDebtRecordDto
            {
                InvoiceId = invoice.InvoiceId,
                RoomId = invoice.RoomId,
                RoomName = invoice.Room.RoomName,
                TenantId = invoice.Room.Contracts
                    .Where(contract => contract.Status.ToLower() == "active" && contract.Tenant != null)
                    .OrderByDescending(contract => contract.StartDate)
                    .Select(contract => contract.TenantId)
                    .FirstOrDefault(),
                TenantName = invoice.Room.Contracts
                    .Where(contract => contract.Status.ToLower() == "active" && contract.Tenant != null)
                    .OrderByDescending(contract => contract.StartDate)
                    .Select(contract => contract.Tenant!.FullName)
                    .FirstOrDefault(),
                PhoneNumber = invoice.Room.Contracts
                    .Where(contract => contract.Status.ToLower() == "active" && contract.Tenant != null)
                    .OrderByDescending(contract => contract.StartDate)
                    .Select(contract => contract.Tenant!.PhoneNumber)
                    .FirstOrDefault(),
                Email = invoice.Room.Contracts
                    .Where(contract => contract.Status.ToLower() == "active" && contract.Tenant != null)
                    .OrderByDescending(contract => contract.StartDate)
                    .Select(contract => contract.Tenant!.Email)
                    .FirstOrDefault(),
                Address = invoice.Room.Contracts
                    .Where(contract => contract.Status.ToLower() == "active" && contract.Tenant != null)
                    .OrderByDescending(contract => contract.StartDate)
                    .Select(contract => contract.Tenant!.Address)
                    .FirstOrDefault(),
                MonthYear = invoice.MonthYear,
                Status = invoice.Status,
                DueDate = invoice.DueDate,
                TotalAmount = invoice.TotalAmount,
                PaidAmount = invoice.Payments
                    .Where(payment => payment.Status == null || payment.Status.ToLower() == "success")
                    .Select(payment => (decimal?)payment.Amount)
                    .Sum()!.GetValueOrDefault(),
                OutstandingAmount = invoice.TotalAmount - invoice.Payments
                    .Where(payment => payment.Status == null || payment.Status.ToLower() == "success")
                    .Select(payment => (decimal?)payment.Amount)
                    .Sum()!.GetValueOrDefault(),
                RoomFee = invoice.RoomFee,
                ElectricFee = invoice.ElectricFee,
                WaterFee = invoice.WaterFee,
                ServiceFee = invoice.ServiceFee,
                ParkingFee = invoice.ParkingFee,
                OtherFee = invoice.OtherFee,
                DiscountAmount = invoice.DiscountAmount,
                Payments = invoice.Payments
                    .Where(payment => payment.Status == null || payment.Status.ToLower() == "success")
                    .Select(payment => new DashboardDebtPaymentRecordDto
                    {
                        Amount = payment.Amount,
                        Note = payment.Note,
                    })
                    .ToList(),
            })
            .ToListAsync();
    }

    public Task<Invoice?> GetInvoiceForPaymentAsync(int invoiceId)
    {
        return _context.Invoices
            .Include(invoice => invoice.Payments)
            .FirstOrDefaultAsync(invoice => invoice.InvoiceId == invoiceId);
    }

    public Task AddPaymentAsync(Payment payment)
    {
        return _context.Payments.AddAsync(payment).AsTask();
    }

    public async Task<decimal> GetMonthlyRevenueAsync(string monthYear, int? buildingId = null)
    {
        var invoiceQuery = _context.Invoices
            .AsNoTracking()
            .AsQueryable();

        if (buildingId.HasValue)
        {
            invoiceQuery = invoiceQuery.Where(invoice => invoice.Room.BuildingId == buildingId.Value);
        }

        return await invoiceQuery
            .Where(invoice => invoice.MonthYear == monthYear)
            .SumAsync(invoice => (decimal?)invoice.TotalAmount) ?? 0m;
    }

    public async Task<List<DashboardRevenueTargetRecordDto>> GetRevenueTargetsAsync(int? buildingId = null)
    {
        var roomQuery = _context.Rooms.AsNoTracking().AsQueryable();
        if (buildingId.HasValue)
        {
            roomQuery = roomQuery.Where(room => room.BuildingId == buildingId.Value);
        }

        return await roomQuery
            .Select(room => new DashboardRevenueTargetRecordDto
            {
                Price = room.Price,
                Status = room.Status,
            })
            .ToListAsync();
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
