using System.Globalization;
using System.Text;
using Backend.Data;
using Backend.DTOs.Dashboard;
using Backend.Entities;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ExcelImportService : Interfaces.IExcelImportService
{
    private const string TemplateDirectoryName = "templates";
    private const string TemplateFileName = "mau-nhap-du-lieu-dashboard.xlsx";

    private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RoomName"] = ["RoomName", "TenPhong", "Tên phòng", "SoPhong", "Số phòng"],
        ["Status"] = ["Status", "TrangThai", "Trạng thái"],
        ["Price"] = ["Price", "GiaPhong", "Giá phòng"],
        ["ElectricPrice"] = ["ElectricPrice", "GiaDien", "Giá điện"],
        ["WaterPrice"] = ["WaterPrice", "GiaNuoc", "Giá nước"],
        ["Area"] = ["Area", "DienTich", "Diện tích"],
        ["MaxPeople"] = ["MaxPeople", "SoNguoiToiDa", "Số người tối đa"],
        ["Description"] = ["Description", "MoTa", "Mô tả"],
        ["FullName"] = ["FullName", "HoTen", "Họ tên"],
        ["PhoneNumber"] = ["PhoneNumber", "SoDienThoai", "Số điện thoại"],
        ["Email"] = ["Email"],
        ["CCCD"] = ["CCCD", "CanCuocCongDan", "Căn cước công dân"],
        ["Address"] = ["Address", "DiaChi", "Địa chỉ"],
        ["MoveInDate"] = ["MoveInDate", "NgayVaoO", "Ngày vào ở"],
        ["Deposit"] = ["Deposit", "TienCoc", "Tiền cọc"],
        ["ContractStartDate"] = ["ContractStartDate", "NgayBatDauHopDong", "Ngày bắt đầu hợp đồng"],
        ["ContractEndDate"] = ["ContractEndDate", "NgayKetThucHopDong", "Ngày kết thúc hợp đồng"],
        ["Note"] = ["Note", "GhiChu", "Ghi chú"],
        ["MonthYear"] = ["MonthYear", "KyHoaDon", "Kỳ hóa đơn"],
        ["RoomFee"] = ["RoomFee", "TienPhong", "Tiền phòng"],
        ["ElectricFee"] = ["ElectricFee", "TienDien", "Tiền điện"],
        ["WaterFee"] = ["WaterFee", "TienNuoc", "Tiền nước"],
        ["ServiceFee"] = ["ServiceFee", "TienDichVu", "Tiền dịch vụ"],
        ["ParkingFee"] = ["ParkingFee", "TienGuiXe", "Tiền gửi xe"],
        ["OtherFee"] = ["OtherFee", "KhoanKhac", "Khoản khác"],
        ["DiscountAmount"] = ["DiscountAmount", "GiamTru", "Giảm trừ"],
        ["TotalAmount"] = ["TotalAmount", "TongTien", "Tổng tiền"],
        ["DueDate"] = ["DueDate", "HanThanhToan", "Hạn thanh toán"],
        ["PaymentDate"] = ["PaymentDate", "NgayThanhToan", "Ngày thanh toán"],
        ["PaidAmount"] = ["PaidAmount", "SoTienDaThu", "Số tiền đã thu"],
    };

    private readonly RentalManagementDb _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IWebHostEnvironment _environment;

    public ExcelImportService(
        RentalManagementDb context,
        IPasswordHasher<User> passwordHasher,
        IWebHostEnvironment environment)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _environment = environment;
    }

    public async Task<ExcelImportResultDto> ImportDashboardSeedAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("Vui lòng chọn file Excel để nhập.");
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Hiện tại hệ thống chỉ hỗ trợ file .xlsx.");
        }

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var result = new ExcelImportResultDto();
        var owner = await EnsureDefaultOwnerAsync(cancellationToken);
        var building = await EnsureDefaultBuildingAsync(owner, cancellationToken);

        var roomsByName = await _context.Rooms
            .Where(room => room.BuildingId == building.BuildingId)
            .ToDictionaryAsync(room => NormalizeKey(room.RoomName), cancellationToken);

        result.RoomsImported = await ImportRoomsAsync(
            FindWorksheet(workbook, "Phòng", "Phong", "Rooms"),
            building.BuildingId,
            roomsByName,
            cancellationToken);

        result.TenantsImported = await ImportTenantsAsync(
            FindWorksheet(workbook, "Khách thuê", "KhachThue", "Tenants"),
            roomsByName,
            result.Warnings,
            result,
            cancellationToken);

        result.InvoicesImported = await ImportInvoicesAsync(
            FindWorksheet(workbook, "Hóa đơn", "HoaDon", "Invoices"),
            owner.UserId,
            roomsByName,
            result.Warnings,
            result,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        result.Message = "Đã nhập dữ liệu Excel thành công. Dashboard có thể làm mới để xem số liệu mới nhất.";
        return result;
    }

    public byte[] GenerateTemplate()
    {
        using var workbook = new XLWorkbook();

        var guideSheet = workbook.Worksheets.Add("Hướng dẫn");
        guideSheet.Cell("A1").Value = "HƯỚNG DẪN NHẬP DỮ LIỆU";
        guideSheet.Cell("A2").Value = "1. Điền dữ liệu theo 3 sheet: Phòng, Khách thuê, Hóa đơn.";
        guideSheet.Cell("A3").Value = "2. Không đổi tên cột tiêu đề ở hàng 1.";
        guideSheet.Cell("A4").Value = "3. Trạng thái phòng: Đang thuê, Phòng trống, Bảo trì.";
        guideSheet.Cell("A5").Value = "4. Trạng thái hóa đơn: Chưa thanh toán, Đã thanh toán một phần, Đã thanh toán.";
        guideSheet.Cell("A6").Value = "5. Kỳ hóa đơn dùng dạng yyyy-MM, ví dụ: 2026-05.";
        guideSheet.Range("A1:A6").Style.Alignment.WrapText = true;
        guideSheet.Column("A").Width = 72;
        guideSheet.Row(1).Style.Font.Bold = true;
        guideSheet.Row(1).Style.Font.FontSize = 16;

        var roomsSheet = workbook.Worksheets.Add("Phòng");
        AddHeader(roomsSheet, new[]
        {
            "Tên phòng", "Trạng thái", "Giá phòng", "Giá điện", "Giá nước", "Diện tích", "Số người tối đa", "Mô tả"
        });
        roomsSheet.Cell(2, 1).Value = "A101";
        roomsSheet.Cell(2, 2).Value = "Đang thuê";
        roomsSheet.Cell(2, 3).Value = 3200000;
        roomsSheet.Cell(2, 4).Value = 3500;
        roomsSheet.Cell(2, 5).Value = 18000;
        roomsSheet.Cell(2, 6).Value = 22;
        roomsSheet.Cell(2, 7).Value = 3;
        roomsSheet.Cell(2, 8).Value = "Phòng có cửa sổ";
        roomsSheet.Cell(3, 1).Value = "A102";
        roomsSheet.Cell(3, 2).Value = "Phòng trống";
        roomsSheet.Cell(3, 3).Value = 3000000;
        roomsSheet.Cell(3, 4).Value = 3500;
        roomsSheet.Cell(3, 5).Value = 18000;

        var tenantsSheet = workbook.Worksheets.Add("Khách thuê");
        AddHeader(tenantsSheet, new[]
        {
            "Họ tên", "Số điện thoại", "Email", "Căn cước công dân", "Địa chỉ", "Ngày vào ở", "Tiền cọc",
            "Tên phòng", "Ngày bắt đầu hợp đồng", "Ngày kết thúc hợp đồng", "Trạng thái", "Ghi chú"
        });
        tenantsSheet.Cell(2, 1).Value = "Nguyễn Văn A";
        tenantsSheet.Cell(2, 2).Value = "0901234567";
        tenantsSheet.Cell(2, 3).Value = "vana@example.com";
        tenantsSheet.Cell(2, 4).Value = "079123456789";
        tenantsSheet.Cell(2, 5).Value = "Quận 9, TP.HCM";
        tenantsSheet.Cell(2, 6).Value = DateTime.Today.AddMonths(-2);
        tenantsSheet.Cell(2, 7).Value = 3000000;
        tenantsSheet.Cell(2, 8).Value = "A101";
        tenantsSheet.Cell(2, 9).Value = DateTime.Today.AddMonths(-2);
        tenantsSheet.Cell(2, 10).Value = DateTime.Today.AddMonths(10);
        tenantsSheet.Cell(2, 11).Value = "Đang ở";
        tenantsSheet.Cell(2, 12).Value = "Khách ở lâu dài";

        var invoicesSheet = workbook.Worksheets.Add("Hóa đơn");
        AddHeader(invoicesSheet, new[]
        {
            "Tên phòng", "Kỳ hóa đơn", "Tiền phòng", "Tiền điện", "Tiền nước", "Tiền dịch vụ", "Tiền gửi xe",
            "Khoản khác", "Giảm trừ", "Tổng tiền", "Hạn thanh toán", "Trạng thái", "Ngày thanh toán",
            "Số tiền đã thu", "Ghi chú"
        });
        invoicesSheet.Cell(2, 1).Value = "A101";
        invoicesSheet.Cell(2, 2).Value = DateTime.Today.ToString("yyyy-MM");
        invoicesSheet.Cell(2, 3).Value = 3200000;
        invoicesSheet.Cell(2, 4).Value = 280000;
        invoicesSheet.Cell(2, 5).Value = 90000;
        invoicesSheet.Cell(2, 6).Value = 150000;
        invoicesSheet.Cell(2, 7).Value = 100000;
        invoicesSheet.Cell(2, 8).Value = 0;
        invoicesSheet.Cell(2, 9).Value = 0;
        invoicesSheet.Cell(2, 10).Value = 3820000;
        invoicesSheet.Cell(2, 11).Value = DateTime.Today.AddDays(7);
        invoicesSheet.Cell(2, 12).Value = "Đã thanh toán một phần";
        invoicesSheet.Cell(2, 13).Value = DateTime.Today;
        invoicesSheet.Cell(2, 14).Value = 2000000;
        invoicesSheet.Cell(2, 15).Value = "Đã thu trước một phần";

        FormatWorksheet(roomsSheet, new[] { 18d, 18d, 16d, 14d, 14d, 14d, 18d, 28d });
        FormatWorksheet(tenantsSheet, new[] { 24d, 18d, 26d, 24d, 24d, 16d, 16d, 16d, 20d, 20d, 16d, 26d });
        FormatWorksheet(invoicesSheet, new[] { 16d, 16d, 16d, 16d, 16d, 16d, 16d, 16d, 16d, 16d, 18d, 16d, 18d, 18d, 28d });

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    public async Task SaveTemplateFileAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("Vui lòng chọn file mẫu Excel để tải lên.");
        }

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ chấp nhận file Excel định dạng .xlsx.");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new InvalidOperationException("File mẫu vượt quá dung lượng cho phép 10MB.");
        }

        await using var validationStream = file.OpenReadStream();
        using (var workbook = new XLWorkbook(validationStream))
        {
            if (!workbook.Worksheets.Any())
            {
                throw new InvalidOperationException("File Excel không hợp lệ hoặc không có sheet dữ liệu.");
            }
        }

        var templatePath = GetTemplateFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(templatePath)!);

        await using var sourceStream = file.OpenReadStream();
        await using var targetStream = new FileStream(templatePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await sourceStream.CopyToAsync(targetStream, cancellationToken);
    }

    public async Task<(byte[] Content, string FileName)> GetTemplateFileAsync(CancellationToken cancellationToken = default)
    {
        var templatePath = GetTemplateFilePath();
        if (File.Exists(templatePath))
        {
            var bytes = await File.ReadAllBytesAsync(templatePath, cancellationToken);
            return (bytes, TemplateFileName);
        }

        return (GenerateTemplate(), TemplateFileName);
    }

    private async Task<int> ImportRoomsAsync(
        IXLWorksheet? worksheet,
        int buildingId,
        Dictionary<string, Room> roomsByName,
        CancellationToken cancellationToken)
    {
        if (worksheet == null)
        {
            return 0;
        }

        var headerMap = BuildHeaderMap(worksheet);
        var imported = 0;

        foreach (var row in GetDataRows(worksheet))
        {
            var roomName = GetString(row, headerMap, "RoomName");
            if (string.IsNullOrWhiteSpace(roomName))
            {
                continue;
            }

            var normalizedKey = NormalizeKey(roomName);
            if (!roomsByName.TryGetValue(normalizedKey, out var room))
            {
                room = new Room
                {
                    BuildingId = buildingId,
                    RoomName = roomName.Trim(),
                };
                await _context.Rooms.AddAsync(room, cancellationToken);
                roomsByName[normalizedKey] = room;
            }

            room.Status = NormalizeRoomStatus(GetString(row, headerMap, "Status"));
            room.Price = GetDecimal(row, headerMap, "Price");
            room.ElectricPrice = GetDecimal(row, headerMap, "ElectricPrice");
            room.WaterPrice = GetDecimal(row, headerMap, "WaterPrice");
            room.Area = GetNullableDouble(row, headerMap, "Area");
            room.MaxPeople = GetNullableInt(row, headerMap, "MaxPeople");
            room.Description = GetString(row, headerMap, "Description");
            room.UpdatedAt = DateTime.UtcNow;

            imported++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task<int> ImportTenantsAsync(
        IXLWorksheet? worksheet,
        Dictionary<string, Room> roomsByName,
        List<string> warnings,
        ExcelImportResultDto summary,
        CancellationToken cancellationToken)
    {
        if (worksheet == null)
        {
            return 0;
        }

        var headerMap = BuildHeaderMap(worksheet);
        var imported = 0;

        var tenants = await _context.Tenants
            .Include(tenant => tenant.Contracts)
            .ToListAsync(cancellationToken);

        foreach (var row in GetDataRows(worksheet))
        {
            var fullName = GetString(row, headerMap, "FullName");
            if (string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            var phone = GetString(row, headerMap, "PhoneNumber");
            var email = GetString(row, headerMap, "Email");
            var cccd = GetString(row, headerMap, "CCCD");

            var tenant = tenants.FirstOrDefault(item =>
                (!string.IsNullOrWhiteSpace(phone) && item.PhoneNumber == phone) ||
                (!string.IsNullOrWhiteSpace(email) && item.Email == email) ||
                (!string.IsNullOrWhiteSpace(cccd) && item.CCCD == cccd));

            if (tenant == null)
            {
                tenant = new Tenant();
                await _context.Tenants.AddAsync(tenant, cancellationToken);
                tenants.Add(tenant);
            }

            tenant.FullName = fullName.Trim();
            tenant.PhoneNumber = NullIfEmpty(phone);
            tenant.Email = NullIfEmpty(email);
            tenant.CCCD = NullIfEmpty(cccd);
            tenant.Address = NullIfEmpty(GetString(row, headerMap, "Address"));
            tenant.MoveInDate = GetNullableDate(row, headerMap, "MoveInDate");
            tenant.IsActive = NormalizeTenantStatus(GetString(row, headerMap, "Status"));
            tenant.Note = NullIfEmpty(GetString(row, headerMap, "Note"));
            tenant.UpdatedAt = DateTime.UtcNow;

            imported++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var row in GetDataRows(worksheet))
        {
            var fullName = GetString(row, headerMap, "FullName");
            if (string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            var phone = GetString(row, headerMap, "PhoneNumber");
            var email = GetString(row, headerMap, "Email");
            var cccd = GetString(row, headerMap, "CCCD");
            var roomName = GetString(row, headerMap, "RoomName");

            if (string.IsNullOrWhiteSpace(roomName))
            {
                continue;
            }

            if (!roomsByName.TryGetValue(NormalizeKey(roomName), out var room))
            {
                warnings.Add($"Không tìm thấy phòng '{roomName}' để gán cho khách '{fullName}'.");
                continue;
            }

            var tenant = tenants.First(item =>
                item.FullName == fullName.Trim() &&
                item.PhoneNumber == NullIfEmpty(phone) &&
                item.Email == NullIfEmpty(email) &&
                item.CCCD == NullIfEmpty(cccd));

            var contractStart = GetNullableDate(row, headerMap, "ContractStartDate") ?? tenant.MoveInDate ?? DateTime.Today;
            var contractEnd = GetNullableDate(row, headerMap, "ContractEndDate") ?? contractStart.AddMonths(12);
            var deposit = GetDecimal(row, headerMap, "Deposit");

            var contract = await _context.Contracts.FirstOrDefaultAsync(item =>
                item.RoomId == room.RoomId &&
                item.TenantId == tenant.TenantId &&
                item.Status.ToLower() == "active", cancellationToken);

            if (contract == null)
            {
                contract = new Contract
                {
                    RoomId = room.RoomId,
                    TenantId = tenant.TenantId,
                };
                await _context.Contracts.AddAsync(contract, cancellationToken);
                summary.ContractsImported++;
            }

            contract.StartDate = contractStart;
            contract.EndDate = contractEnd;
            contract.Deposit = deposit;
            contract.Status = "Active";
            contract.Note = NullIfEmpty(GetString(row, headerMap, "Note"));

            room.Status = "occupied";
            room.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task<int> ImportInvoicesAsync(
        IXLWorksheet? worksheet,
        int ownerUserId,
        Dictionary<string, Room> roomsByName,
        List<string> warnings,
        ExcelImportResultDto summary,
        CancellationToken cancellationToken)
    {
        if (worksheet == null)
        {
            return 0;
        }

        var headerMap = BuildHeaderMap(worksheet);
        var imported = 0;

        var roomIds = roomsByName.Values.Select(room => room.RoomId).Where(id => id > 0).ToList();
        var invoices = await _context.Invoices
            .Include(invoice => invoice.Payments)
            .Where(invoice => roomIds.Contains(invoice.RoomId))
            .ToListAsync(cancellationToken);

        var invoiceMap = invoices
            .GroupBy(invoice => $"{invoice.RoomId}:{invoice.MonthYear}")
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var row in GetDataRows(worksheet))
        {
            var roomName = GetString(row, headerMap, "RoomName");
            var monthYear = GetString(row, headerMap, "MonthYear");

            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(monthYear))
            {
                continue;
            }

            if (!roomsByName.TryGetValue(NormalizeKey(roomName), out var room))
            {
                warnings.Add($"Không tìm thấy phòng '{roomName}' để nhập hóa đơn.");
                continue;
            }

            var normalizedMonthYear = NormalizeMonthYear(monthYear);
            var invoiceKey = $"{room.RoomId}:{normalizedMonthYear}";
            if (!invoiceMap.TryGetValue(invoiceKey, out var invoice))
            {
                invoice = new Invoice
                {
                    RoomId = room.RoomId,
                    UserId = ownerUserId,
                    MonthYear = normalizedMonthYear,
                };
                await _context.Invoices.AddAsync(invoice, cancellationToken);
                invoiceMap[invoiceKey] = invoice;
            }

            invoice.RoomFee = GetDecimal(row, headerMap, "RoomFee");
            invoice.ElectricFee = GetDecimal(row, headerMap, "ElectricFee");
            invoice.WaterFee = GetDecimal(row, headerMap, "WaterFee");
            invoice.ServiceFee = GetDecimal(row, headerMap, "ServiceFee");
            invoice.ParkingFee = GetDecimal(row, headerMap, "ParkingFee");
            invoice.OtherFee = GetDecimal(row, headerMap, "OtherFee");
            invoice.DiscountAmount = GetDecimal(row, headerMap, "DiscountAmount");
            invoice.TotalAmount = GetDecimal(row, headerMap, "TotalAmount");
            invoice.DueDate = GetNullableDate(row, headerMap, "DueDate");
            invoice.Note = NullIfEmpty(GetString(row, headerMap, "Note"));

            var paidAmount = GetDecimal(row, headerMap, "PaidAmount");
            invoice.Status = NormalizeInvoiceStatus(GetString(row, headerMap, "Status"), paidAmount, invoice.TotalAmount);
            invoice.PaymentDate = paidAmount > 0 ? GetNullableDate(row, headerMap, "PaymentDate") ?? DateTime.Now : null;

            if (invoice.Payments.Any())
            {
                _context.Payments.RemoveRange(invoice.Payments);
                invoice.Payments.Clear();
            }

            if (paidAmount > 0)
            {
                invoice.Payments.Add(new Payment
                {
                    Amount = paidAmount,
                    PaymentMethod = "Bank",
                    PaymentDate = invoice.PaymentDate ?? DateTime.Now,
                    Status = "Success",
                    Note = "Nhập từ Excel",
                });
                summary.PaymentsImported++;
            }

            imported++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task<User> EnsureDefaultOwnerAsync(CancellationToken cancellationToken)
    {
        var ownerRole = await _context.Roles.FirstOrDefaultAsync(role => role.Name == "Owner", cancellationToken);
        if (ownerRole == null)
        {
            ownerRole = new Role
            {
                Name = "Owner",
                Description = "Chu tro"
            };
            await _context.Roles.AddAsync(ownerRole, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var owner = await _context.Users.FirstOrDefaultAsync(user => user.RoleId == ownerRole.RoleId, cancellationToken);
        if (owner != null)
        {
            return owner;
        }

        owner = new User
        {
            RoleId = ownerRole.RoleId,
            FullName = "Chủ trọ nhập Excel",
            Email = "excel-owner@local.test",
            PhoneNumber = "0999999999",
            Address = "Dữ liệu khởi tạo từ Excel",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        owner.PasswordHash = _passwordHasher.HashPassword(owner, "Owner@123");

        await _context.Users.AddAsync(owner, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return owner;
    }

    private async Task<Building> EnsureDefaultBuildingAsync(User owner, CancellationToken cancellationToken)
    {
        var building = await _context.Buildings.FirstOrDefaultAsync(item => item.UserId == owner.UserId, cancellationToken);
        if (building != null)
        {
            return building;
        }

        building = new Building
        {
            UserId = owner.UserId,
            BuildingName = "Khu trọ nhập Excel",
            Address = "Chưa cập nhật địa chỉ",
            Description = "Tự tạo khi nhập dữ liệu Excel",
            CreatedAt = DateTime.UtcNow,
        };

        await _context.Buildings.AddAsync(building, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return building;
    }

    private static IXLWorksheet? FindWorksheet(XLWorkbook workbook, params string[] possibleNames)
    {
        return workbook.Worksheets.FirstOrDefault(sheet =>
            possibleNames.Any(name => NormalizeKey(sheet.Name) == NormalizeKey(name)));
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
    {
        return worksheet.Row(1)
            .CellsUsed()
            .ToDictionary(cell => NormalizeKey(cell.GetString()), cell => cell.Address.ColumnNumber);
    }

    private static IEnumerable<IXLRow> GetDataRows(IXLWorksheet worksheet)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (!row.CellsUsed().Any())
            {
                continue;
            }

            yield return row;
        }
    }

    private static string GetString(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string columnName)
    {
        if (!TryGetColumnNumber(headerMap, columnName, out var columnNumber))
        {
            return string.Empty;
        }

        return row.Cell(columnNumber).GetValue<string>().Trim();
    }

    private static decimal GetDecimal(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string columnName)
    {
        var raw = GetString(row, headerMap, columnName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 0m;
        }

        raw = raw.Replace(",", string.Empty);
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out number)
                ? number
                : 0m;
    }

    private static int? GetNullableInt(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string columnName)
    {
        var raw = GetString(row, headerMap, columnName);
        return int.TryParse(raw, out var number) ? number : null;
    }

    private static double? GetNullableDouble(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string columnName)
    {
        var raw = GetString(row, headerMap, columnName);
        return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : double.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out number)
                ? number
                : null;
    }

    private static DateTime? GetNullableDate(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string columnName)
    {
        if (!TryGetColumnNumber(headerMap, columnName, out var columnNumber))
        {
            return null;
        }

        var cell = row.Cell(columnNumber);
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue<DateTime>(out var date))
        {
            return date;
        }

        var raw = cell.GetString().Trim();
        return DateTime.TryParse(raw, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out date)
            ? date
            : DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                ? date
                : null;
    }

    private static bool TryGetColumnNumber(IReadOnlyDictionary<string, int> headerMap, string columnName, out int columnNumber)
    {
        if (headerMap.TryGetValue(NormalizeKey(columnName), out columnNumber))
        {
            return true;
        }

        if (!HeaderAliases.TryGetValue(columnName, out var aliases))
        {
            columnNumber = 0;
            return false;
        }

        foreach (var alias in aliases)
        {
            if (headerMap.TryGetValue(NormalizeKey(alias), out columnNumber))
            {
                return true;
            }
        }

        columnNumber = 0;
        return false;
    }

    private static string NormalizeRoomStatus(string? status)
    {
        return NormalizeKey(status) switch
        {
            "occupied" or "rented" or "dangthue" => "occupied",
            "maintenance" or "baotri" => "maintenance",
            "available" or "vacant" or "empty" or "trong" or "phongtrong" => "vacant",
            _ => "vacant",
        };
    }

    private static bool NormalizeTenantStatus(string? status)
    {
        return NormalizeKey(status) switch
        {
            "inactive" or "movedout" or "disabled" or "ngungthue" or "daroidi" => false,
            _ => true,
        };
    }

    private static string NormalizeInvoiceStatus(string? status, decimal paidAmount, decimal totalAmount)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            return NormalizeKey(status) switch
            {
                "chuathanhtoan" => "Unpaid",
                "dathanhtoanmotphan" => "Partial",
                "dathanhtoan" => "Paid",
                _ => status.Trim(),
            };
        }

        if (paidAmount <= 0)
        {
            return "Unpaid";
        }

        if (paidAmount >= totalAmount)
        {
            return "Paid";
        }

        return "Partial";
    }

    private static string NormalizeMonthYear(string rawValue)
    {
        if (DateTime.TryParse(rawValue, out var date))
        {
            return date.ToString("yyyy-MM");
        }

        return rawValue.Trim();
    }

    private static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (char.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark &&
                !char.IsWhiteSpace(character) &&
                character != '_' &&
                character != '-')
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string GetTemplateFilePath()
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", TemplateDirectoryName, TemplateFileName);
    }

    private static void AddHeader(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            var cell = worksheet.Cell(1, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 13;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#265073");
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#B7C9E2");
        }
    }

    private static void FormatWorksheet(IXLWorksheet worksheet, IReadOnlyList<double> columnWidths)
    {
        worksheet.SheetView.FreezeRows(1);
        worksheet.Row(1).Height = 34;
        worksheet.Row(1).Style.Alignment.WrapText = true;

        for (var index = 0; index < columnWidths.Count; index++)
        {
            worksheet.Column(index + 1).Width = columnWidths[index];
        }

        var usedRange = worksheet.RangeUsed();
        if (usedRange != null)
        {
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E1E8F0");
            usedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
    }
}
