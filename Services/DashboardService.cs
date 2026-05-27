using Backend.DTOs.Dashboard;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using ClosedXML.Excel;

namespace Backend.Services;

public class DashboardService : IDashboardService
{
    private const string DebtItemNotePrefix = "DebtItem=";

    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int month, int year, int? buildingId = null)
    {
        var roomStats = await GetRoomStatsAsync(buildingId);
        var debtInfo = await GetDebtInfoAsync(buildingId);
        var revenue = await GetRevenueAsync(month, year, buildingId);

        return new DashboardStatsDto
        {
            TotalRooms = roomStats.TotalRooms,
            OccupiedRooms = roomStats.OccupiedRooms,
            EmptyRooms = roomStats.EmptyRooms,
            MonthlyRevenue = revenue.MonthlyRevenue,
            UnpaidTenantsCount = debtInfo.UnpaidTenantsCount,
            TotalDebt = debtInfo.TotalDebt,
        };
    }

    public async Task<DashboardRoomStatsDto> GetRoomStatsAsync(int? buildingId = null)
    {
        var rooms = await _dashboardRepository.GetRoomStatusesAsync(buildingId);

        return new DashboardRoomStatsDto
        {
            TotalRooms = rooms.Count,
            OccupiedRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "occupied"),
            EmptyRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "vacant"),
            MaintenanceRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "maintenance"),
        };
    }

    public async Task<DashboardDebtInfoDto> GetDebtInfoAsync(int? buildingId = null)
    {
        var debtRows = await _dashboardRepository.GetDebtRecordsAsync(buildingId);

        var groupedDebts = debtRows
            .Where(row => row.OutstandingAmount > 0 || HasTaggedDebtPayment(row))
            .GroupBy(row => new
            {
                row.RoomId,
                row.RoomName,
                row.TenantId,
                row.TenantName,
                row.PhoneNumber,
                row.Email,
                row.Address,
            })
            .Select(group => new DashboardDebtorDto
            {
                RoomId = group.Key.RoomId,
                TenantId = group.Key.TenantId,
                Name = string.IsNullOrWhiteSpace(group.Key.TenantName) ? $"Phòng {group.Key.RoomName}" : group.Key.TenantName!,
                Room = group.Key.RoomName,
                PhoneNumber = group.Key.PhoneNumber,
                Email = group.Key.Email,
                Address = group.Key.Address,
                Amount = group.Sum(item => item.OutstandingAmount),
                DebtMonths = group
                    .OrderBy(item => item.MonthYear)
                    .Select(item => new DashboardDebtMonthDto
                    {
                        InvoiceId = item.InvoiceId,
                        MonthYear = item.MonthYear,
                        TotalAmount = item.TotalAmount,
                        PaidAmount = item.PaidAmount,
                        OutstandingAmount = item.OutstandingAmount,
                        RoomFee = item.RoomFee,
                        ElectricFee = item.ElectricFee,
                        WaterFee = item.WaterFee,
                        ServiceFee = item.ServiceFee,
                        ParkingFee = item.ParkingFee,
                        OtherFee = item.OtherFee,
                        DiscountAmount = item.DiscountAmount,
                        Status = item.Status,
                        DueDate = item.DueDate,
                        DebtItems = BuildDebtItems(item),
                    })
                    .ToList(),
            })
            .OrderByDescending(item => item.Amount)
            .ToList();

        return new DashboardDebtInfoDto
        {
            UnpaidTenantsCount = groupedDebts.Count(item => item.Amount > 0),
            UnpaidRoomsCount = groupedDebts.Count(item => item.Amount > 0),
            TotalDebt = groupedDebts.Sum(item => item.Amount),
            Debtors = groupedDebts,
            TopDebtors = groupedDebts.Where(item => item.Amount > 0).Take(5).ToList(),
        };
    }

    public async Task<DashboardDebtPaymentResultDto> RecordDebtPaymentAsync(int invoiceId, DashboardDebtPaymentRequestDto request)
    {
        var invoice = await _dashboardRepository.GetInvoiceForPaymentAsync(invoiceId)
            ?? throw new InvalidOperationException("Không tìm thấy hóa đơn cần gạch nợ.");

        var paidAmount = CalculatePaidAmount(invoice);
        var outstandingAmount = Math.Max(invoice.TotalAmount - paidAmount, 0m);
        var debtItemKey = NormalizeDebtItemKey(request.DebtItemKey);
        var selectedDebtItem = string.IsNullOrWhiteSpace(debtItemKey)
            ? null
            : BuildDebtItems(invoice).FirstOrDefault(item => item.ItemKey.Equals(debtItemKey, StringComparison.OrdinalIgnoreCase));

        if (outstandingAmount <= 0)
        {
            invoice.Status = "Paid";
            invoice.PaymentDate ??= DateTime.Now;
            await _dashboardRepository.SaveChangesAsync();

            return new DashboardDebtPaymentResultDto
            {
                InvoiceId = invoice.InvoiceId,
                Status = invoice.Status,
                PaidAmount = paidAmount,
                OutstandingAmount = 0m,
                Message = "Hóa đơn này đã được thu đủ trước đó.",
            };
        }

        if (!string.IsNullOrWhiteSpace(debtItemKey) && selectedDebtItem is null)
        {
            throw new InvalidOperationException("Không tìm thấy khoản nợ cần gạch trong hóa đơn này.");
        }

        var targetOutstandingAmount = selectedDebtItem?.OutstandingAmount ?? outstandingAmount;
        var paymentAmount = request.Amount ?? targetOutstandingAmount;
        if (paymentAmount <= 0)
        {
            throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");
        }

        if (paymentAmount > targetOutstandingAmount)
        {
            throw new InvalidOperationException("Số tiền thanh toán không được lớn hơn số tiền còn nợ.");
        }

        var paymentDate = request.PaymentDate ?? DateTime.Now;
        var note = BuildPaymentNote(request.Note, selectedDebtItem);
        await _dashboardRepository.AddPaymentAsync(new Payment
        {
            InvoiceId = invoice.InvoiceId,
            Amount = paymentAmount,
            PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod.Trim(),
            PaymentDate = paymentDate,
            Status = "Success",
            Note = note,
        });

        var newPaidAmount = paidAmount + paymentAmount;
        var newOutstandingAmount = Math.Max(invoice.TotalAmount - newPaidAmount, 0m);

        invoice.Status = newOutstandingAmount <= 0 ? "Paid" : "Partial";
        invoice.PaymentDate = newOutstandingAmount <= 0 ? paymentDate : null;

        await _dashboardRepository.SaveChangesAsync();

        return new DashboardDebtPaymentResultDto
        {
            InvoiceId = invoice.InvoiceId,
            Status = invoice.Status,
            PaidAmount = newPaidAmount,
            OutstandingAmount = newOutstandingAmount,
            Message = newOutstandingAmount <= 0
                ? "Đã gạch nợ và cập nhật hóa đơn đã thu."
                : "Đã ghi nhận thanh toán một phần, hóa đơn vẫn còn nợ.",
        };
    }

    public async Task<DashboardDebtPaymentResultDto> RestoreDebtItemAsync(int invoiceId, string itemKey)
    {
        var invoice = await _dashboardRepository.GetInvoiceForPaymentAsync(invoiceId)
            ?? throw new InvalidOperationException("Không tìm thấy hóa đơn cần khôi phục.");

        var normalizedItemKey = NormalizeDebtItemKey(itemKey)
            ?? throw new InvalidOperationException("Không tìm thấy khoản cần khôi phục.");

        var debtItem = BuildDebtItems(invoice)
            .FirstOrDefault(item => item.ItemKey.Equals(normalizedItemKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Không tìm thấy khoản cần khôi phục trong hóa đơn này.");

        var taggedPayments = invoice.Payments
            .Where(payment =>
                (payment.Status == null || payment.Status.Equals("Success", StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(ParseDebtItemKey(payment.Note), normalizedItemKey, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (taggedPayments.Count == 0)
        {
            throw new InvalidOperationException("Khoản này chưa có giao dịch gạch nợ riêng để khôi phục.");
        }

        foreach (var payment in taggedPayments)
        {
            payment.Status = "Cancelled";
            payment.Note = $"{payment.Note}; Đã khôi phục khoản nợ";
        }

        var paidAmount = CalculatePaidAmount(invoice);
        var outstandingAmount = Math.Max(invoice.TotalAmount - paidAmount, 0m);

        invoice.Status = outstandingAmount <= 0
            ? "Paid"
            : paidAmount <= 0
                ? "Unpaid"
                : "Partial";
        invoice.PaymentDate = outstandingAmount <= 0 ? invoice.PaymentDate : null;

        await _dashboardRepository.SaveChangesAsync();

        return new DashboardDebtPaymentResultDto
        {
            InvoiceId = invoice.InvoiceId,
            Status = invoice.Status,
            PaidAmount = paidAmount,
            OutstandingAmount = outstandingAmount,
            Message = $"Đã khôi phục khoản {debtItem.ItemName}.",
        };
    }

    public async Task<DashboardRevenueDto> GetRevenueAsync(int month, int year, int? buildingId = null)
    {
        var monthYear = FormatMonthYear(month, year);
        var monthlyRevenue = await _dashboardRepository.GetMonthlyRevenueAsync(monthYear, buildingId);
        var roomValues = await _dashboardRepository.GetRevenueTargetsAsync(buildingId);

        var targetRevenue = roomValues
            .Where(room => NormalizeRoomStatus(room.Status) != "maintenance")
            .Sum(room => room.Price);

        return new DashboardRevenueDto
        {
            MonthlyRevenue = monthlyRevenue,
            TargetRevenue = targetRevenue,
        };
    }

    public async Task<(byte[] Content, string FileName)> ExportDashboardExcelAsync(int month, int year, int? buildingId = null)
    {
        var roomStats = await GetRoomStatsAsync(buildingId);
        var debtInfo = await GetDebtInfoAsync(buildingId);
        var revenue = await GetRevenueAsync(month, year, buildingId);

        using var workbook = new XLWorkbook();
        var overviewSheet = workbook.Worksheets.Add("Tong quan");
        var roomSheet = workbook.Worksheets.Add("Tinh trang phong");
        var debtSheet = workbook.Worksheets.Add("Can thu");

        overviewSheet.Cell(1, 1).Value = "Bao cao dashboard nha tro";
        overviewSheet.Cell(2, 1).Value = "Ky bao cao";
        overviewSheet.Cell(2, 2).Value = $"{year:D4}-{month:D2}";
        overviewSheet.Cell(4, 1).Value = "Chi so";
        overviewSheet.Cell(4, 2).Value = "Gia tri";

        overviewSheet.Cell(5, 1).Value = "Tong so phong";
        overviewSheet.Cell(5, 2).Value = roomStats.TotalRooms;
        overviewSheet.Cell(6, 1).Value = "Phong dang thue";
        overviewSheet.Cell(6, 2).Value = roomStats.OccupiedRooms;
        overviewSheet.Cell(7, 1).Value = "Phong trong";
        overviewSheet.Cell(7, 2).Value = roomStats.EmptyRooms;
        overviewSheet.Cell(8, 1).Value = "Phong bao tri";
        overviewSheet.Cell(8, 2).Value = roomStats.MaintenanceRooms;
        overviewSheet.Cell(9, 1).Value = "Tong hoa don thang nay";
        overviewSheet.Cell(9, 2).Value = revenue.MonthlyRevenue;
        overviewSheet.Cell(10, 1).Value = "Can thu thang nay";
        overviewSheet.Cell(10, 2).Value = debtInfo.TotalDebt;
        overviewSheet.Cell(11, 1).Value = "Da thu";
        overviewSheet.Cell(11, 2).Value = Math.Max(revenue.MonthlyRevenue - Math.Min(debtInfo.TotalDebt, revenue.MonthlyRevenue), 0);
        overviewSheet.Cell(12, 1).Value = "Khach chua thanh toan";
        overviewSheet.Cell(12, 2).Value = debtInfo.UnpaidTenantsCount;

        roomSheet.Cell(1, 1).Value = "Tong so phong";
        roomSheet.Cell(1, 2).Value = "Dang thue";
        roomSheet.Cell(1, 3).Value = "Phong trong";
        roomSheet.Cell(1, 4).Value = "Bao tri";
        roomSheet.Cell(2, 1).Value = roomStats.TotalRooms;
        roomSheet.Cell(2, 2).Value = roomStats.OccupiedRooms;
        roomSheet.Cell(2, 3).Value = roomStats.EmptyRooms;
        roomSheet.Cell(2, 4).Value = roomStats.MaintenanceRooms;

        debtSheet.Cell(1, 1).Value = "Ten khach / phong";
        debtSheet.Cell(1, 2).Value = "Phong";
        debtSheet.Cell(1, 3).Value = "So tien can thu";

        if (debtInfo.TopDebtors.Count == 0)
        {
            debtSheet.Cell(2, 1).Value = "Khong co du lieu can thu noi bat";
        }
        else
        {
            for (var index = 0; index < debtInfo.TopDebtors.Count; index++)
            {
                var debtor = debtInfo.TopDebtors[index];
                debtSheet.Cell(index + 2, 1).Value = debtor.Name;
                debtSheet.Cell(index + 2, 2).Value = debtor.Room;
                debtSheet.Cell(index + 2, 3).Value = debtor.Amount;
            }
        }

        overviewSheet.Cell(9, 2).Style.NumberFormat.Format = "#,##0";
        overviewSheet.Cell(10, 2).Style.NumberFormat.Format = "#,##0";
        overviewSheet.Cell(11, 2).Style.NumberFormat.Format = "#,##0";
        debtSheet.Column(3).Style.NumberFormat.Format = "#,##0";

        StyleWorksheet(overviewSheet);
        StyleWorksheet(roomSheet);
        StyleWorksheet(debtSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return (
            stream.ToArray(),
            $"dashboard-{year:D4}-{month:D2}.xlsx");
    }

    private static void StyleWorksheet(IXLWorksheet worksheet)
    {
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return;
        }

        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Font.FontName = "Segoe UI";

        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF1FF");

        if (worksheet.Name == "Tong quan")
        {
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(4, 1, 4, 2).Style.Font.Bold = true;
            worksheet.Range(4, 1, 4, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F8");
        }

        worksheet.Columns().AdjustToContents();
    }

    private static string FormatMonthYear(int month, int year) => $"{year:D4}-{month:D2}";

    private static bool HasTaggedDebtPayment(DashboardDebtRecordDto record)
    {
        return record.Payments.Any(payment => !string.IsNullOrWhiteSpace(ParseDebtItemKey(payment.Note)));
    }

    private static List<DashboardDebtItemDto> BuildDebtItems(DashboardDebtRecordDto record)
    {
        return BuildDebtItems(
            record.TotalAmount,
            record.DiscountAmount,
            CreateDebtItemSeeds(
                record.RoomFee,
                record.ElectricFee,
                record.WaterFee,
                record.ServiceFee,
                record.ParkingFee,
                record.OtherFee),
            record.Payments);
    }

    private static List<DashboardDebtItemDto> BuildDebtItems(Invoice invoice)
    {
        var payments = invoice.Payments
            .Where(payment => payment.Status == null || payment.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))
            .Select(payment => new DashboardDebtPaymentRecordDto
            {
                Amount = payment.Amount,
                Note = payment.Note,
            })
            .ToList();

        return BuildDebtItems(
            invoice.TotalAmount,
            invoice.DiscountAmount,
            CreateDebtItemSeeds(
                invoice.RoomFee,
                invoice.ElectricFee,
                invoice.WaterFee,
                invoice.ServiceFee,
                invoice.ParkingFee,
                invoice.OtherFee),
            payments);
    }

    private static List<DashboardDebtItemDto> BuildDebtItems(
        decimal totalAmount,
        decimal discountAmount,
        List<DebtItemSeed> seeds,
        IEnumerable<DashboardDebtPaymentRecordDto> payments)
    {
        ApplyDiscountToDebtItems(seeds, discountAmount);
        AdjustDebtItemsToTotalAmount(seeds, totalAmount);

        var paymentList = payments.ToList();
        var untaggedPaymentAmount = paymentList
            .Where(payment => string.IsNullOrWhiteSpace(ParseDebtItemKey(payment.Note)))
            .Sum(payment => payment.Amount);

        var taggedPaymentAmounts = paymentList
            .Select(payment => new
            {
                ItemKey = ParseDebtItemKey(payment.Note),
                payment.Amount,
            })
            .Where(payment => !string.IsNullOrWhiteSpace(payment.ItemKey))
            .GroupBy(payment => payment.ItemKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(payment => payment.Amount), StringComparer.OrdinalIgnoreCase);

        var result = new List<DashboardDebtItemDto>();
        foreach (var seed in seeds.Where(item => item.Amount > 0))
        {
            var taggedPaidAmount = Math.Min(seed.Amount, taggedPaymentAmounts.GetValueOrDefault(seed.ItemKey));
            var remainingItemAmount = Math.Max(seed.Amount - taggedPaidAmount, 0m);
            var untaggedPaidAmount = Math.Min(remainingItemAmount, untaggedPaymentAmount);
            untaggedPaymentAmount -= untaggedPaidAmount;

            var paidAmount = taggedPaidAmount + untaggedPaidAmount;
            result.Add(new DashboardDebtItemDto
            {
                ItemKey = seed.ItemKey,
                ItemName = seed.ItemName,
                Amount = seed.Amount,
                PaidAmount = paidAmount,
                OutstandingAmount = Math.Max(seed.Amount - paidAmount, 0m),
                CanRestore = taggedPaidAmount > 0,
            });
        }

        return result;
    }

    private static List<DebtItemSeed> CreateDebtItemSeeds(
        decimal roomFee,
        decimal electricFee,
        decimal waterFee,
        decimal serviceFee,
        decimal parkingFee,
        decimal otherFee)
    {
        return new List<DebtItemSeed>
        {
            new("room", "Tiền trọ", roomFee),
            new("electric", "Tiền điện", electricFee),
            new("water", "Tiền nước", waterFee),
            new("service", "Dịch vụ", serviceFee),
            new("parking", "Gửi xe", parkingFee),
            new("other", "Khoản khác", otherFee),
        };
    }

    private static void ApplyDiscountToDebtItems(List<DebtItemSeed> seeds, decimal discountAmount)
    {
        var remainingDiscount = Math.Max(discountAmount, 0m);
        for (var index = seeds.Count - 1; index >= 0 && remainingDiscount > 0; index--)
        {
            var discountForItem = Math.Min(seeds[index].Amount, remainingDiscount);
            seeds[index].Amount -= discountForItem;
            remainingDiscount -= discountForItem;
        }
    }

    private static void AdjustDebtItemsToTotalAmount(List<DebtItemSeed> seeds, decimal totalAmount)
    {
        var remainingDifference = Math.Max(seeds.Sum(item => item.Amount) - Math.Max(totalAmount, 0m), 0m);
        for (var index = seeds.Count - 1; index >= 0 && remainingDifference > 0; index--)
        {
            var reduction = Math.Min(seeds[index].Amount, remainingDifference);
            seeds[index].Amount -= reduction;
            remainingDifference -= reduction;
        }
    }

    private static string? ParseDebtItemKey(string? note)
    {
        if (string.IsNullOrWhiteSpace(note) || !note.StartsWith(DebtItemNotePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var value = note[DebtItemNotePrefix.Length..];
        var separatorIndex = value.IndexOf(';');
        return NormalizeDebtItemKey(separatorIndex >= 0 ? value[..separatorIndex] : value);
    }

    private static string? NormalizeDebtItemKey(string? itemKey)
    {
        if (string.IsNullOrWhiteSpace(itemKey))
        {
            return null;
        }

        return itemKey.Trim().ToLowerInvariant();
    }

    private static string BuildPaymentNote(string? note, DashboardDebtItemDto? selectedDebtItem)
    {
        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (selectedDebtItem is null)
        {
            return trimmedNote ?? "Gạch nợ từ trang công nợ";
        }

        return $"{DebtItemNotePrefix}{selectedDebtItem.ItemKey}; {trimmedNote ?? $"Gạch nợ khoản {selectedDebtItem.ItemName} từ trang công nợ"}";
    }

    private static decimal CalculatePaidAmount(Invoice invoice)
    {
        return invoice.Payments
            .Where(payment => payment.Status == null || payment.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))
            .Sum(payment => payment.Amount);
    }

    private sealed class DebtItemSeed
    {
        public DebtItemSeed(string itemKey, string itemName, decimal amount)
        {
            ItemKey = itemKey;
            ItemName = itemName;
            Amount = Math.Max(amount, 0m);
        }

        public string ItemKey { get; }
        public string ItemName { get; }
        public decimal Amount { get; set; }
    }

    private static string NormalizeRoomStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "vacant";
        }

        return status.Trim().ToLower() switch
        {
            "available" => "vacant",
            "empty" => "vacant",
            "vacant" => "vacant",
            "occupied" => "occupied",
            "rented" => "occupied",
            "maintenance" => "maintenance",
            _ => status.Trim().ToLower(),
        };
    }
}
