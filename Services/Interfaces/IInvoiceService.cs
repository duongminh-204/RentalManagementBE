using Backend.DTOs.Invoices;

namespace Backend.Services.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDto> GenerateInvoiceFromUtilityUsageAsync(CreateInvoiceFromUtilityUsageDto dto);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int invoiceId);
    Task<InvoiceDto?> GetInvoiceByRoomAndMonthAsync(int roomId, string monthYear);
    Task<IEnumerable<InvoiceDto>> GenerateInvoicesForMonthAsync(string monthYear, int? buildingId = null);
    Task<IEnumerable<InvoiceDto>> SearchInvoicesAsync(int? roomId = null, string? tenantName = null, string? monthYearFrom = null, string? monthYearTo = null, string? status = null, string? search = null);
}
