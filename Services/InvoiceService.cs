using Backend.DTOs.Invoices;
using Backend.Entities;
using Backend.Interfaces;
using Backend.Services.Interfaces;
using Backend.Repositories.Interfaces;

namespace Backend.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public InvoiceService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<InvoiceDto> GenerateInvoiceFromUtilityUsageAsync(CreateInvoiceFromUtilityUsageDto dto, int? ownerUserId = null)
    {
        var monthYear = NormalizeMonthYear(dto.MonthYear);
        ValidateUsage(dto);

        var room = await _invoiceRepository.GetRoomWithDetailsAsync(dto.RoomId);
        if (room == null)
            throw new KeyNotFoundException($"Không tìm thấy phòng với ID = {dto.RoomId}.");

        if (ownerUserId.HasValue && room.Building?.UserId != ownerUserId.Value)
            throw new KeyNotFoundException($"Khong tim thay phong voi ID = {dto.RoomId}.");

        var user = await _invoiceRepository.GetUserByIdAsync(dto.UserId);
        if (user == null)
            throw new KeyNotFoundException($"Không tìm thấy người dùng với ID = {dto.UserId}.");

        var usage = await _invoiceRepository.GetUtilityUsageAsync(dto.RoomId, monthYear);
        if (usage == null)
        {
            usage = new UtilityUsage
            {
                RoomId = dto.RoomId,
                MonthYear = monthYear,
                ElectricNumberBf = dto.ElectricNumberBf,
                ElectricNumberAt = dto.ElectricNumberAt,
                WaterNumberBf = dto.WaterNumberBf,
                WaterNumberAt = dto.WaterNumberAt
            };
            await _invoiceRepository.AddUtilityUsageAsync(usage);
        }
        else
        {
            usage.ElectricNumberBf = dto.ElectricNumberBf;
            usage.ElectricNumberAt = dto.ElectricNumberAt;
            usage.WaterNumberBf = dto.WaterNumberBf;
            usage.WaterNumberAt = dto.WaterNumberAt;
            await _invoiceRepository.UpdateUtilityUsageAsync(usage);
        }

        await _invoiceRepository.SaveChangesAsync();

        var electricConsumed = Math.Max(0, dto.ElectricNumberAt - dto.ElectricNumberBf);
        var waterConsumed = Math.Max(0, dto.WaterNumberAt - dto.WaterNumberBf);
        var roomFee = room.Price;
        var electricFee = electricConsumed * room.ElectricPrice;
        var waterFee = waterConsumed * room.WaterPrice;
        var serviceFee = room.RoomServices
            .Where(rs => rs.Service != null)
            .Sum(rs => rs.Service.UnitPrice);

        var parkingFee = dto.ParkingFeeOverride ?? room.Vehicles
            .Where(v => string.Equals(v.Status, "active", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(v.Status))
            .Sum(v => v.ParkingFee);

        var totalAmount = roomFee + electricFee + waterFee + serviceFee + parkingFee + dto.OtherFee - dto.DiscountAmount;
        totalAmount = Math.Max(totalAmount, 0m);

        var invoice = await _invoiceRepository.GetInvoiceByRoomAndMonthAsync(dto.RoomId, monthYear, ownerUserId);
        if (invoice == null)
        {
            invoice = new Invoice
            {
                RoomId = room.RoomId,
                UserId = dto.UserId,
                MonthYear = monthYear,
                RoomFee = roomFee,
                ElectricFee = electricFee,
                WaterFee = waterFee,
                ServiceFee = serviceFee,
                ParkingFee = parkingFee,
                OtherFee = dto.OtherFee,
                DiscountAmount = dto.DiscountAmount,
                TotalAmount = totalAmount,
                Status = "Unpaid",
                DueDate = GetDueDate(monthYear),
                Note = dto.Note,
                CreatedAt = DateTime.Now,
                InvoiceDetails = BuildInvoiceDetails(
                    electricConsumed,
                    waterConsumed,
                    roomFee,
                    room.ElectricPrice,
                    waterFee,
                    room.WaterPrice,
                    electricFee,
                    serviceFee,
                    parkingFee,
                    dto.OtherFee,
                    dto.DiscountAmount,
                    room).ToList()
            };

            await _invoiceRepository.AddInvoiceAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();
        }
        else
        {
            if (!dto.ForceRecreate && string.Equals(invoice.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Hoá đơn đã được thanh toán và không thể chỉnh sửa.");

            invoice.RoomFee = roomFee;
            invoice.ElectricFee = electricFee;
            invoice.WaterFee = waterFee;
            invoice.ServiceFee = serviceFee;
            invoice.ParkingFee = parkingFee;
            invoice.OtherFee = dto.OtherFee;
            invoice.DiscountAmount = dto.DiscountAmount;
            invoice.TotalAmount = totalAmount;
            invoice.Status = "Unpaid";
            invoice.DueDate = GetDueDate(monthYear);
            invoice.Note = dto.Note;
            invoice.InvoiceDetails.Clear();
            foreach (var detail in BuildInvoiceDetails(
                electricConsumed,
                waterConsumed,
                roomFee,
                room.ElectricPrice,
                waterFee,
                room.WaterPrice,
                electricFee,
                serviceFee,
                parkingFee,
                dto.OtherFee,
                dto.DiscountAmount,
                room))
            {
                invoice.InvoiceDetails.Add(detail);
            }

            await _invoiceRepository.UpdateInvoiceAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();
        }

        invoice.QRCodeUrl = BuildQrCodeUrl(invoice.InvoiceId, invoice.TotalAmount);
        await _invoiceRepository.UpdateInvoiceAsync(invoice);
        await _invoiceRepository.SaveChangesAsync();

        return MapToDto(invoice, electricConsumed, waterConsumed);
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int invoiceId, int? ownerUserId = null)
    {
        var invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId, ownerUserId);
        if (invoice == null)
            return null;

        var usage = await _invoiceRepository.GetUtilityUsageAsync(invoice.RoomId, invoice.MonthYear);
        var electricConsumed = usage == null ? 0 : Math.Max(0, usage.ElectricNumberAt - usage.ElectricNumberBf);
        var waterConsumed = usage == null ? 0 : Math.Max(0, usage.WaterNumberAt - usage.WaterNumberBf);

        return MapToDto(invoice, electricConsumed, waterConsumed);
    }

    public async Task<InvoiceDto?> GetInvoiceByRoomAndMonthAsync(int roomId, string monthYear, int? ownerUserId = null)
    {
        var normalized = NormalizeMonthYear(monthYear);
        var invoice = await _invoiceRepository.GetInvoiceByRoomAndMonthAsync(roomId, normalized, ownerUserId);
        if (invoice == null)
            return null;

        var usage = await _invoiceRepository.GetUtilityUsageAsync(roomId, normalized);
        var electricConsumed = usage == null ? 0 : Math.Max(0, usage.ElectricNumberAt - usage.ElectricNumberBf);
        var waterConsumed = usage == null ? 0 : Math.Max(0, usage.WaterNumberAt - usage.WaterNumberBf);

        return MapToDto(invoice, electricConsumed, waterConsumed);
    }

    public async Task<IEnumerable<InvoiceDto>> GenerateInvoicesForMonthAsync(string monthYear, int? buildingId = null, int? ownerUserId = null)
    {
        var normalized = NormalizeMonthYear(monthYear);
        var rooms = await _invoiceRepository.GetRoomsWithDetailsAsync(buildingId, ownerUserId);
        var result = new List<InvoiceDto>();

        foreach (var room in rooms)
        {
            var usage = await _invoiceRepository.GetUtilityUsageAsync(room.RoomId, normalized);
            if (usage == null)
                continue;

            var request = new CreateInvoiceFromUtilityUsageDto
            {
                RoomId = room.RoomId,
                MonthYear = normalized,
                UserId = await GetDefaultInvoiceCreatorIdAsync(),
                ElectricNumberBf = usage.ElectricNumberBf,
                ElectricNumberAt = usage.ElectricNumberAt,
                WaterNumberBf = usage.WaterNumberBf,
                WaterNumberAt = usage.WaterNumberAt,
                OtherFee = 0m,
                DiscountAmount = 0m
            };

            result.Add(await GenerateInvoiceFromUtilityUsageAsync(request, ownerUserId));
        }

        return result;
    }

    public async Task<IEnumerable<InvoiceDto>> SearchInvoicesAsync(int? roomId = null, string? tenantName = null, string? monthYearFrom = null, string? monthYearTo = null, string? status = null, string? search = null, int? ownerUserId = null)
    {
        var invoices = await _invoiceRepository.SearchInvoicesAsync(roomId, tenantName, monthYearFrom, monthYearTo, status, search, ownerUserId);
        var results = new List<InvoiceDto>();

        foreach (var invoice in invoices)
        {
            var usage = await _invoiceRepository.GetUtilityUsageAsync(invoice.RoomId, invoice.MonthYear);
            var electricConsumed = usage == null ? 0 : Math.Max(0, usage.ElectricNumberAt - usage.ElectricNumberBf);
            var waterConsumed = usage == null ? 0 : Math.Max(0, usage.WaterNumberAt - usage.WaterNumberBf);
            results.Add(MapToDto(invoice, electricConsumed, waterConsumed));
        }

        return results;
    }

    public async Task DeleteInvoiceAsync(int invoiceId, int? ownerUserId = null)
    {
        var deleted = await _invoiceRepository.DeleteInvoiceAsync(invoiceId, ownerUserId);
        if (!deleted)
            throw new KeyNotFoundException("Không tìm thấy hóa đơn.");
    }

    private static void ValidateUsage(CreateInvoiceFromUtilityUsageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.MonthYear))
            throw new ArgumentException("Tháng/năm không được để trống.");

        if (dto.ElectricNumberAt < dto.ElectricNumberBf)
            throw new ArgumentException("Số điện cuối kỳ phải lớn hơn hoặc bằng số đầu kỳ.");

        if (dto.WaterNumberAt < dto.WaterNumberBf)
            throw new ArgumentException("Số nước cuối kỳ phải lớn hơn hoặc bằng số đầu kỳ.");
    }

    private static string NormalizeMonthYear(string monthYear)
    {
        if (DateTime.TryParseExact(monthYear, "yyyy-MM", null, System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed.ToString("yyyy-MM");

        if (DateTime.TryParseExact(monthYear, "MM/yyyy", null, System.Globalization.DateTimeStyles.None, out parsed))
            return parsed.ToString("yyyy-MM");

        if (DateTime.TryParse(monthYear, out parsed))
            return parsed.ToString("yyyy-MM");

        throw new ArgumentException("Định dạng tháng/năm không hợp lệ. Hãy dùng yyyy-MM hoặc MM/yyyy.");
    }

    private static DateTime GetDueDate(string monthYear)
    {
        if (!DateTime.TryParseExact(monthYear + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var firstOfMonth))
            firstOfMonth = DateTime.Today;

        return firstOfMonth.AddMonths(1).AddDays(5);
    }

    private static IEnumerable<InvoiceDetail> BuildInvoiceDetails(
        int electricConsumed,
        int waterConsumed,
        decimal roomFee,
        decimal electricUnitPrice,
        decimal waterFee,
        decimal waterUnitPrice,
        decimal electricFee,
        decimal serviceFee,
        decimal parkingFee,
        decimal otherFee,
        decimal discountAmount,
        Room room)
    {
        var details = new List<InvoiceDetail>
        {
            new InvoiceDetail
            {
                ItemName = "Tiền phòng",
                Quantity = 1,
                UnitPrice = roomFee,
                Amount = roomFee
            },
            new InvoiceDetail
            {
                ItemName = $"Tiền điện ({electricConsumed} số)",
                Quantity = electricConsumed,
                UnitPrice = electricUnitPrice,
                Amount = electricFee
            },
            new InvoiceDetail
            {
                ItemName = $"Tiền nước ({waterConsumed} khối)",
                Quantity = waterConsumed,
                UnitPrice = waterUnitPrice,
                Amount = waterFee
            }
        };

        foreach (var roomService in room.RoomServices.Where(rs => rs.Service != null))
        {
            details.Add(new InvoiceDetail
            {
                ItemName = roomService.Service.ServiceName,
                Quantity = 1,
                UnitPrice = roomService.Service.UnitPrice,
                Amount = roomService.Service.UnitPrice
            });
        }

        if (parkingFee > 0)
        {
            details.Add(new InvoiceDetail
            {
                ItemName = "Phí bãi xe",
                Quantity = 1,
                UnitPrice = parkingFee,
                Amount = parkingFee
            });
        }

        if (otherFee > 0)
        {
            details.Add(new InvoiceDetail
            {
                ItemName = "Phí khác",
                Quantity = 1,
                UnitPrice = otherFee,
                Amount = otherFee
            });
        }

        if (discountAmount > 0)
        {
            details.Add(new InvoiceDetail
            {
                ItemName = "Giảm giá",
                Quantity = 1,
                UnitPrice = -discountAmount,
                Amount = -discountAmount
            });
        }

        return details;
    }

    private static string BuildQrCodeUrl(int invoiceId, decimal amount)
    {
        return $"https://payment.example.com/pay?invoice={invoiceId}&amount={amount:0.00}";
    }

    private async Task<int> GetDefaultInvoiceCreatorIdAsync()
    {
        var user = await _invoiceRepository.GetAnyUserAsync();
        return user?.UserId ?? throw new InvalidOperationException("Không tìm thấy người dùng để tạo hoá đơn.");
    }

    private static string? GetInvoiceTenantName(Invoice invoice)
    {
        return invoice.Room?.Contracts?
            .Where(c => c.Tenant != null)
            .OrderByDescending(c => c.EndDate)
            .ThenByDescending(c => c.CreatedAt)
            .Select(c => c.Tenant!.FullName)
            .FirstOrDefault();
    }

    private static InvoiceDto MapToDto(Invoice invoice, int electricConsumed, int waterConsumed)
    {
        return new InvoiceDto
        {
            InvoiceId = invoice.InvoiceId,
            RoomId = invoice.RoomId,
            UserId = invoice.UserId,
            MonthYear = invoice.MonthYear,
            ElectricConsumed = electricConsumed,
            WaterConsumed = waterConsumed,
            RoomFee = invoice.RoomFee,
            ElectricFee = invoice.ElectricFee,
            WaterFee = invoice.WaterFee,
            ServiceFee = invoice.ServiceFee,
            ParkingFee = invoice.ParkingFee,
            OtherFee = invoice.OtherFee,
            DiscountAmount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            AmountDue = invoice.TotalAmount,
            RoomName = invoice.Room?.RoomName,
            TenantName = GetInvoiceTenantName(invoice),
            Status = invoice.Status,
            DueDate = invoice.DueDate,
            QRCodeUrl = invoice.QRCodeUrl,
            Note = invoice.Note,
            CreatedAt = invoice.CreatedAt,
            InvoiceDetails = invoice.InvoiceDetails.Select(detail => new InvoiceDetailDto
            {
                InvoiceDetailId = detail.InvoiceDetailId,
                ItemName = detail.ItemName,
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice,
                Amount = detail.Amount
            }).ToList()
        };
    }
}
