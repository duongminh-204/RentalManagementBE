using Backend.DTOs.Invoices;

namespace Backend.Services.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDto> GenerateInvoiceFromUtilityUsageAsync(CreateInvoiceFromUtilityUsageDto dto, int? ownerUserId = null);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int invoiceId, int? ownerUserId = null);
    Task<InvoiceDto?> GetInvoiceByRoomAndMonthAsync(int roomId, string monthYear, int? ownerUserId = null);
    Task<IEnumerable<InvoiceDto>> GenerateInvoicesForMonthAsync(string monthYear, int? buildingId = null, int? ownerUserId = null);
    Task<IEnumerable<InvoiceDto>> SearchInvoicesAsync(int? roomId = null, string? tenantName = null, string? monthYearFrom = null, string? monthYearTo = null, string? status = null, string? search = null, int? ownerUserId = null);
}
