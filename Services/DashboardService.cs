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
    private readonly IExcelImportRepository _excelImportRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IExcelImportRepository excelImportRepository)
    {
        _dashboardRepository = dashboardRepository;
        _excelImportRepository = excelImportRepository;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int month, int year, int? buildingId = null, int? ownerUserId = null)
    {
        var roomStats = await GetRoomStatsAsync(buildingId, ownerUserId);
        var debtInfo = await GetDebtInfoAsync(buildingId, ownerUserId);
        var revenue = await GetRevenueAsync(month, year, buildingId, ownerUserId);

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

    public async Task<DashboardRoomStatsDto> GetRoomStatsAsync(int? buildingId = null, int? ownerUserId = null)
    {
        var rooms = await _dashboardRepository.GetRoomStatusesAsync(buildingId, ownerUserId);

        return new DashboardRoomStatsDto
        {
            TotalRooms = rooms.Count,
            OccupiedRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "occupied"),
            EmptyRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "vacant"),
            MaintenanceRooms = rooms.Count(room => NormalizeRoomStatus(room.Status) == "maintenance"),
        };
    }

    public async Task<DashboardDebtInfoDto> GetDebtInfoAsync(int? buildingId = null, int? ownerUserId = null)
    {
        var debtRows = await _dashboardRepository.GetDebtRecordsAsync(buildingId, ownerUserId);

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

    public async Task<DashboardRevenueDto> GetRevenueAsync(int month, int year, int? buildingId = null, int? ownerUserId = null)
    {
        var monthYear = FormatMonthYear(month, year);
        var monthlyRevenue = await _dashboardRepository.GetMonthlyRevenueAsync(monthYear, buildingId, ownerUserId);
        var roomValues = await _dashboardRepository.GetRevenueTargetsAsync(buildingId, ownerUserId);

        var targetRevenue = roomValues
            .Where(room => NormalizeRoomStatus(room.Status) != "maintenance")
            .Sum(room => room.Price);

        return new DashboardRevenueDto
        {
            MonthlyRevenue = monthlyRevenue,
            TargetRevenue = targetRevenue,
        };
    }

    public async Task<(byte[] Content, string FileName)> ExportDashboardExcelAsync(int month, int year, int? buildingId = null, int? ownerUserId = null)
    {
        var roomStats = await GetRoomStatsAsync(buildingId, ownerUserId);
        var debtInfo = await GetDebtInfoAsync(buildingId, ownerUserId);
        var revenue = await GetRevenueAsync(month, year, buildingId, ownerUserId);
        var buildings = await TryLoadExportRowsAsync(() => _excelImportRepository.ListBuildingsWithOwnerAsync());
        var rooms = await TryLoadExportRowsAsync(() => _excelImportRepository.ListRoomsWithDetailsAsync(buildingId));
        var tenants = await TryLoadExportRowsAsync(() => _excelImportRepository.ListTenantsWithVehiclesAndContractsAsync());
        var contracts = await TryLoadExportRowsAsync(() => _excelImportRepository.ListContractsWithRoomAndTenantAsync(buildingId));
        var vehicles = await TryLoadExportRowsAsync(() => _excelImportRepository.ListVehiclesWithRoomAndTenantAsync(buildingId));
        var deviceCatalogs = await TryLoadExportRowsAsync(() => _excelImportRepository.ListDeviceCatalogsAsync());
        var devices = await TryLoadExportRowsAsync(() => _excelImportRepository.ListDevicesWithRoomAndCatalogAsync(buildingId));
        var services = await TryLoadExportRowsAsync(() => _excelImportRepository.ListServicesWithRoomsAsync());
        var invoices = await TryLoadExportRowsAsync(() => _excelImportRepository.ListInvoicesWithRoomAndPaymentsAsync(buildingId));

        using var workbook = new XLWorkbook();
        var overviewSheet = workbook.Worksheets.Add("Tổng quan");
        var roomSheet = workbook.Worksheets.Add("Tình trạng phòng");
        var debtSheet = workbook.Worksheets.Add("Cần thu");

        overviewSheet.Cell(1, 1).Value = "Báo cáo dashboard nhà trọ";
        overviewSheet.Cell(2, 1).Value = "Kỳ báo cáo";
        overviewSheet.Cell(2, 2).Value = $"{year:D4}-{month:D2}";
        overviewSheet.Cell(4, 1).Value = "Chỉ số";
        overviewSheet.Cell(4, 2).Value = "Giá trị";

        overviewSheet.Cell(5, 1).Value = "Tổng số phòng";
        overviewSheet.Cell(5, 2).Value = roomStats.TotalRooms;
        overviewSheet.Cell(6, 1).Value = "Phòng đang thuê";
        overviewSheet.Cell(6, 2).Value = roomStats.OccupiedRooms;
        overviewSheet.Cell(7, 1).Value = "Phòng trống";
        overviewSheet.Cell(7, 2).Value = roomStats.EmptyRooms;
        overviewSheet.Cell(8, 1).Value = "Phòng bảo trì";
        overviewSheet.Cell(8, 2).Value = roomStats.MaintenanceRooms;
        overviewSheet.Cell(9, 1).Value = "Tổng hóa đơn tháng này";
        overviewSheet.Cell(9, 2).Value = revenue.MonthlyRevenue;
        overviewSheet.Cell(10, 1).Value = "Cần thu tháng này";
        overviewSheet.Cell(10, 2).Value = debtInfo.TotalDebt;
        overviewSheet.Cell(11, 1).Value = "Đã thu";
        overviewSheet.Cell(11, 2).Value = Math.Max(revenue.MonthlyRevenue - Math.Min(debtInfo.TotalDebt, revenue.MonthlyRevenue), 0);
        overviewSheet.Cell(12, 1).Value = "Khách chưa thanh toán";
        overviewSheet.Cell(12, 2).Value = debtInfo.UnpaidTenantsCount;

        roomSheet.Cell(1, 1).Value = "Tổng số phòng";
        roomSheet.Cell(1, 2).Value = "Đang thuê";
        roomSheet.Cell(1, 3).Value = "Phòng trống";
        roomSheet.Cell(1, 4).Value = "Bảo trì";
        roomSheet.Cell(2, 1).Value = roomStats.TotalRooms;
        roomSheet.Cell(2, 2).Value = roomStats.OccupiedRooms;
        roomSheet.Cell(2, 3).Value = roomStats.EmptyRooms;
        roomSheet.Cell(2, 4).Value = roomStats.MaintenanceRooms;

        debtSheet.Cell(1, 1).Value = "Tên khách / phòng";
        debtSheet.Cell(1, 2).Value = "Phòng";
        debtSheet.Cell(1, 3).Value = "Số tiền cần thu";

        if (debtInfo.TopDebtors.Count == 0)
        {
            debtSheet.Cell(2, 1).Value = "Không có dữ liệu cần thu nổi bật";
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
        AddDataSheets(
            workbook,
            buildings,
            rooms,
            tenants,
            contracts,
            vehicles,
            deviceCatalogs,
            devices,
            services,
            invoices);

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

        if (worksheet.Name == "Tổng quan")
        {
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(4, 1, 4, 2).Style.Font.Bold = true;
            worksheet.Range(4, 1, 4, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F8");
        }

        worksheet.Columns().AdjustToContents();
    }

    private static void AddDataSheets(
        XLWorkbook workbook,
        IReadOnlyList<Building> buildings,
        IReadOnlyList<Room> rooms,
        IReadOnlyList<Tenant> tenants,
        IReadOnlyList<Contract> contracts,
        IReadOnlyList<Vehicle> vehicles,
        IReadOnlyList<DeviceCatalog> deviceCatalogs,
        IReadOnlyList<Device> devices,
        IReadOnlyList<Service> services,
        IReadOnlyList<Invoice> invoices)
    {
        var buildingsSheet = workbook.Worksheets.Add("Tòa nhà");
        AddHeader(buildingsSheet, "Tên tòa nhà", "Chủ trọ", "Địa chỉ", "Mô tả");
        for (var index = 0; index < buildings.Count; index++)
        {
            var row = index + 2;
            var building = buildings[index];
            buildingsSheet.Cell(row, 1).Value = building.BuildingName;
            buildingsSheet.Cell(row, 2).Value = building.User?.FullName;
            buildingsSheet.Cell(row, 3).Value = building.Address;
            buildingsSheet.Cell(row, 4).Value = building.Description;
        }

        var roomsSheet = workbook.Worksheets.Add("Phòng trọ");
        AddHeader(roomsSheet, "Tên tòa nhà", "Tên phòng", "Trạng thái", "Giá phòng", "Giá điện", "Giá nước", "Diện tích", "Số người tối đa", "Mô tả");
        for (var index = 0; index < rooms.Count; index++)
        {
            var row = index + 2;
            var room = rooms[index];
            roomsSheet.Cell(row, 1).Value = room.Building?.BuildingName;
            roomsSheet.Cell(row, 2).Value = room.RoomName;
            roomsSheet.Cell(row, 3).Value = room.Status;
            roomsSheet.Cell(row, 4).Value = room.Price;
            roomsSheet.Cell(row, 5).Value = room.ElectricPrice;
            roomsSheet.Cell(row, 6).Value = room.WaterPrice;
            roomsSheet.Cell(row, 7).Value = room.Area;
            roomsSheet.Cell(row, 8).Value = room.MaxPeople;
            roomsSheet.Cell(row, 9).Value = room.Description;
        }

        var tenantsSheet = workbook.Worksheets.Add("Khách thuê");
        AddHeader(tenantsSheet, "Họ tên", "Số điện thoại", "Email", "CCCD", "Ngày sinh", "Giới tính", "Nghề nghiệp", "Nơi làm việc", "Địa chỉ", "Ngày vào ở", "Ngày rời đi", "Trạng thái", "Ghi chú");
        for (var index = 0; index < tenants.Count; index++)
        {
            var row = index + 2;
            var tenant = tenants[index];
            tenantsSheet.Cell(row, 1).Value = tenant.FullName;
            tenantsSheet.Cell(row, 2).Value = tenant.PhoneNumber;
            tenantsSheet.Cell(row, 3).Value = tenant.Email;
            tenantsSheet.Cell(row, 4).Value = tenant.CCCD;
            tenantsSheet.Cell(row, 5).Value = tenant.DateOfBirth;
            tenantsSheet.Cell(row, 6).Value = tenant.Gender;
            tenantsSheet.Cell(row, 7).Value = tenant.Occupation;
            tenantsSheet.Cell(row, 8).Value = tenant.Workplace;
            tenantsSheet.Cell(row, 9).Value = tenant.Address;
            tenantsSheet.Cell(row, 10).Value = tenant.MoveInDate;
            tenantsSheet.Cell(row, 11).Value = tenant.MoveOutDate;
            tenantsSheet.Cell(row, 12).Value = tenant.IsActive ? "Active" : "Inactive";
            tenantsSheet.Cell(row, 13).Value = tenant.Note;
        }

        var contractsSheet = workbook.Worksheets.Add("Hợp đồng");
        AddHeader(contractsSheet, "Tên phòng", "Họ tên khách", "Ngày bắt đầu", "Ngày kết thúc", "Giá thuê", "Tiền cọc", "Chu kỳ thanh toán", "Trạng thái cọc", "Hoàn cọc", "Khấu trừ cọc", "Trạng thái", "Ngày chấm dứt", "Lý do chấm dứt", "Ghi chú");
        for (var index = 0; index < contracts.Count; index++)
        {
            var row = index + 2;
            var contract = contracts[index];
            contractsSheet.Cell(row, 1).Value = contract.Room?.RoomName;
            contractsSheet.Cell(row, 2).Value = contract.Tenant?.FullName;
            contractsSheet.Cell(row, 3).Value = contract.StartDate;
            contractsSheet.Cell(row, 4).Value = contract.EndDate;
            contractsSheet.Cell(row, 5).Value = contract.RentPrice;
            contractsSheet.Cell(row, 6).Value = contract.Deposit;
            contractsSheet.Cell(row, 7).Value = contract.PaymentCycle;
            contractsSheet.Cell(row, 8).Value = contract.DepositStatus;
            contractsSheet.Cell(row, 9).Value = contract.DepositRefundAmount;
            contractsSheet.Cell(row, 10).Value = contract.DepositDeductionAmount;
            contractsSheet.Cell(row, 11).Value = contract.Status;
            contractsSheet.Cell(row, 12).Value = contract.TerminatedAt;
            contractsSheet.Cell(row, 13).Value = contract.TerminationReason;
            contractsSheet.Cell(row, 14).Value = contract.Note;
        }

        var vehiclesSheet = workbook.Worksheets.Add("Phương tiện");
        AddHeader(vehiclesSheet, "Biển số", "Loại xe", "Hãng xe", "Màu sắc", "Họ tên khách", "Tên phòng", "Phí gửi xe", "Trạng thái", "Ngày đăng ký", "Ghi chú");
        for (var index = 0; index < vehicles.Count; index++)
        {
            var row = index + 2;
            var vehicle = vehicles[index];
            vehiclesSheet.Cell(row, 1).Value = vehicle.LicensePlateNumber;
            vehiclesSheet.Cell(row, 2).Value = vehicle.VehicleType;
            vehiclesSheet.Cell(row, 3).Value = vehicle.Brand;
            vehiclesSheet.Cell(row, 4).Value = vehicle.Color;
            vehiclesSheet.Cell(row, 5).Value = vehicle.Tenant?.FullName;
            vehiclesSheet.Cell(row, 6).Value = vehicle.Room?.RoomName;
            vehiclesSheet.Cell(row, 7).Value = vehicle.ParkingFee;
            vehiclesSheet.Cell(row, 8).Value = vehicle.Status;
            vehiclesSheet.Cell(row, 9).Value = vehicle.RegistrationDate;
            vehiclesSheet.Cell(row, 10).Value = vehicle.Notes;
        }

        var deviceCatalogSheet = workbook.Worksheets.Add("Danh mục thiết bị");
        AddHeader(deviceCatalogSheet, "Tên thiết bị", "Icon");
        for (var index = 0; index < deviceCatalogs.Count; index++)
        {
            var row = index + 2;
            deviceCatalogSheet.Cell(row, 1).Value = deviceCatalogs[index].Name;
            deviceCatalogSheet.Cell(row, 2).Value = deviceCatalogs[index].Icon;
        }

        var devicesSheet = workbook.Worksheets.Add("Thiết bị phòng");
        AddHeader(devicesSheet, "Tên phòng", "Tên thiết bị", "Danh mục thiết bị", "Số lượng", "Trạng thái", "Ảnh");
        for (var index = 0; index < devices.Count; index++)
        {
            var row = index + 2;
            var device = devices[index];
            devicesSheet.Cell(row, 1).Value = device.Room?.RoomName;
            devicesSheet.Cell(row, 2).Value = device.DeviceName;
            devicesSheet.Cell(row, 3).Value = device.DeviceCatalog?.Name;
            devicesSheet.Cell(row, 4).Value = device.Quantity;
            devicesSheet.Cell(row, 5).Value = device.Status;
            devicesSheet.Cell(row, 6).Value = device.ImageUrl;
        }

        var servicesSheet = workbook.Worksheets.Add("Dịch vụ");
        AddHeader(servicesSheet, "Tên dịch vụ", "Đơn giá", "Chu kỳ tính", "Đơn vị", "Icon");
        for (var index = 0; index < services.Count; index++)
        {
            var row = index + 2;
            var service = services[index];
            servicesSheet.Cell(row, 1).Value = service.ServiceName;
            servicesSheet.Cell(row, 2).Value = service.UnitPrice;
            servicesSheet.Cell(row, 3).Value = service.BillingCycle;
            servicesSheet.Cell(row, 4).Value = service.Unit;
            servicesSheet.Cell(row, 5).Value = service.Icon;
        }

        var roomServicesSheet = workbook.Worksheets.Add("Dịch vụ phòng");
        AddHeader(roomServicesSheet, "Tên phòng", "Tên dịch vụ");
        var roomServiceRow = 2;
        foreach (var service in services)
        {
            foreach (var roomService in service.RoomServices.OrderBy(item => item.Room?.RoomName))
            {
                roomServicesSheet.Cell(roomServiceRow, 1).Value = roomService.Room?.RoomName;
                roomServicesSheet.Cell(roomServiceRow, 2).Value = service.ServiceName;
                roomServiceRow++;
            }
        }

        var invoicesSheet = workbook.Worksheets.Add("Hóa đơn");
        AddHeader(invoicesSheet, "Tên phòng", "Kỳ hóa đơn", "Tiền phòng", "Tiền điện", "Tiền nước", "Tiền dịch vụ", "Tiền gửi xe", "Khoản khác", "Giảm trừ", "Tổng tiền", "Hạn thanh toán", "Trạng thái", "Ngày thanh toán", "Số tiền đã thu", "Ghi chú");
        for (var index = 0; index < invoices.Count; index++)
        {
            var row = index + 2;
            var invoice = invoices[index];
            invoicesSheet.Cell(row, 1).Value = invoice.Room?.RoomName;
            invoicesSheet.Cell(row, 2).Value = invoice.MonthYear;
            invoicesSheet.Cell(row, 3).Value = invoice.RoomFee;
            invoicesSheet.Cell(row, 4).Value = invoice.ElectricFee;
            invoicesSheet.Cell(row, 5).Value = invoice.WaterFee;
            invoicesSheet.Cell(row, 6).Value = invoice.ServiceFee;
            invoicesSheet.Cell(row, 7).Value = invoice.ParkingFee;
            invoicesSheet.Cell(row, 8).Value = invoice.OtherFee;
            invoicesSheet.Cell(row, 9).Value = invoice.DiscountAmount;
            invoicesSheet.Cell(row, 10).Value = invoice.TotalAmount;
            invoicesSheet.Cell(row, 11).Value = invoice.DueDate;
            invoicesSheet.Cell(row, 12).Value = invoice.Status;
            invoicesSheet.Cell(row, 13).Value = invoice.PaymentDate;
            invoicesSheet.Cell(row, 14).Value = invoice.Payments.Sum(payment => payment.Amount);
            invoicesSheet.Cell(row, 15).Value = invoice.Note;
        }

        foreach (var worksheet in workbook.Worksheets.Where(sheet => sheet.Name != "Tổng quan" && sheet.Name != "Tình trạng phòng" && sheet.Name != "Cần thu"))
        {
            StyleDataWorksheet(worksheet);
        }
    }

    private static async Task<List<T>> TryLoadExportRowsAsync<T>(Func<Task<List<T>>> loadRows)
    {
        try
        {
            return await loadRows();
        }
        catch (Exception ex) when (IsMissingSchemaError(ex))
        {
            return new List<T>();
        }
    }

    private static bool IsMissingSchemaError(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddHeader(IXLWorksheet worksheet, params string[] headers)
    {
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }
    }

    private static void StyleDataWorksheet(IXLWorksheet worksheet)
    {
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return;
        }

        worksheet.SheetView.FreezeRows(1);
        usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        usedRange.Style.Font.FontName = "Segoe UI";
        usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E1E8F0");

        var headerRange = worksheet.Range(1, 1, 1, usedRange.ColumnCount());
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#265073");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.WrapText = true;

        worksheet.Row(1).Height = 32;
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
