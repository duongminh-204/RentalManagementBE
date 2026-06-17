using System.Globalization;
using System.Text;
using Backend.DTOs.Dashboard;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;

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

    static ExcelImportService()
    {
        HeaderAliases["BuildingName"] = ["BuildingName", "TenToaNha", "Ten toa nha"];
        HeaderAliases["FullName"] = HeaderAliases["FullName"].Concat(["HoTenKhach", "Ho ten khach"]).ToArray();
        HeaderAliases["DateOfBirth"] = ["DateOfBirth", "NgaySinh", "Ngay sinh"];
        HeaderAliases["Gender"] = ["Gender", "GioiTinh", "Gioi tinh"];
        HeaderAliases["Occupation"] = ["Occupation", "NgheNghiep", "Nghe nghiep"];
        HeaderAliases["Workplace"] = ["Workplace", "NoiLamViec", "Noi lam viec"];
        HeaderAliases["MoveOutDate"] = ["MoveOutDate", "NgayRoiDi", "Ngay roi di"];
        HeaderAliases["ContractStartDate"] = HeaderAliases["ContractStartDate"].Concat(["NgayBatDau", "Ngay bat dau"]).ToArray();
        HeaderAliases["ContractEndDate"] = HeaderAliases["ContractEndDate"].Concat(["NgayKetThuc", "Ngay ket thuc"]).ToArray();
        HeaderAliases["RentPrice"] = ["RentPrice", "GiaThue", "Gia thue"];
        HeaderAliases["BillingCycle"] = ["BillingCycle", "ChuKyTinh", "Chu ky tinh", "ChuKyThanhToan", "Chu ky thanh toan"];
        HeaderAliases["DepositStatus"] = ["DepositStatus", "TrangThaiCoc", "Trang thai coc"];
        HeaderAliases["VehicleType"] = ["VehicleType", "LoaiXe", "Loai xe"];
        HeaderAliases["Brand"] = ["Brand", "HangXe", "Hang xe"];
        HeaderAliases["Color"] = ["Color", "MauSac", "Mau sac"];
        HeaderAliases["LicensePlateNumber"] = ["LicensePlateNumber", "BienSo", "Bien so"];
        HeaderAliases["RegistrationDate"] = ["RegistrationDate", "NgayDangKy", "Ngay dang ky"];
        HeaderAliases["Notes"] = ["Notes", "GhiChu", "Ghi chu"];
        HeaderAliases["DeviceName"] = ["DeviceName", "TenThietBi", "Ten thiet bi"];
        HeaderAliases["DeviceCatalogName"] = ["DeviceCatalogName", "DanhMucThietBi", "Danh muc thiet bi"];
        HeaderAliases["Quantity"] = ["Quantity", "SoLuong", "So luong"];
        HeaderAliases["ImageUrl"] = ["ImageUrl", "Anh", "Image"];
        HeaderAliases["Icon"] = ["Icon"];
        HeaderAliases["ServiceName"] = ["ServiceName", "TenDichVu", "Ten dich vu"];
        HeaderAliases["UnitPrice"] = ["UnitPrice", "DonGia", "Don gia"];
        HeaderAliases["Unit"] = ["Unit", "DonVi", "Don vi"];
    }

    private readonly IExcelImportRepository _excelImportRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IFileStorageService _fileStorage;

    public ExcelImportService(
        IExcelImportRepository excelImportRepository,
        IPasswordHasher<User> passwordHasher,
        IFileStorageService fileStorage)
    {
        _excelImportRepository = excelImportRepository;
        _passwordHasher = passwordHasher;
        _fileStorage = fileStorage;
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

        await using var transaction = await _excelImportRepository.BeginTransactionAsync(cancellationToken);

        var result = new ExcelImportResultDto();
        var owner = await EnsureDefaultOwnerAsync(cancellationToken);
        var building = await EnsureDefaultBuildingAsync(owner, cancellationToken);

        await ImportBuildingAsync(
            FindWorksheet(workbook, "Toa nha", "Buildings"),
            building,
            cancellationToken);

        var roomsByName = await _excelImportRepository.GetRoomsByBuildingAsync(building.BuildingId, NormalizeKey, cancellationToken);

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

        var tenants = await _excelImportRepository.GetTenantsWithContractsAsync(cancellationToken);

        result.ContractsImported += await ImportContractsAsync(
            FindWorksheet(workbook, "Hop dong", "Contracts"),
            roomsByName,
            tenants,
            result.Warnings,
            cancellationToken);

        result.VehiclesImported = await ImportVehiclesAsync(
            FindWorksheet(workbook, "Phuong tien", "Vehicles"),
            roomsByName,
            tenants,
            result.Warnings,
            cancellationToken);

        await ImportDeviceCatalogsAsync(
            FindWorksheet(workbook, "Danh muc thiet bi", "DeviceCatalogs"),
            cancellationToken);

        result.DevicesImported = await ImportDevicesAsync(
            FindWorksheet(workbook, "Thiet bi phong", "Devices"),
            roomsByName,
            result.Warnings,
            cancellationToken);

        result.ServicesImported = await ImportServicesAsync(
            FindWorksheet(workbook, "Dich vu", "Services"),
            cancellationToken);

        await ImportRoomServicesAsync(
            FindWorksheet(workbook, "Dich vu phong", "RoomServices"),
            roomsByName,
            result.Warnings,
            cancellationToken);

        result.InvoicesImported = await ImportInvoicesAsync(
            FindWorksheet(workbook, "Hóa đơn", "HoaDon", "Invoices"),
            owner.UserId,
            roomsByName,
            result.Warnings,
            result,
            cancellationToken);

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
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

        await using var sourceStream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await sourceStream.CopyToAsync(memoryStream, cancellationToken);
        await _fileStorage.SaveBytesAsync(memoryStream.ToArray(), TemplateDirectoryName, TemplateFileName, cancellationToken);
    }

    public async Task<(byte[] Content, string FileName)> GetTemplateFileAsync(CancellationToken cancellationToken = default)
    {
        var savedTemplate = await _fileStorage.ReadBytesAsync(TemplateDirectoryName, TemplateFileName, cancellationToken);
        return (savedTemplate ?? GenerateFullWorkbookTemplate(), TemplateFileName);
    }

    private static byte[] GenerateFullWorkbookTemplate()
    {
        using var workbook = new XLWorkbook();

        var guideSheet = workbook.Worksheets.Add("Hướng dẫn");
        guideSheet.Cell("A1").Value = "HƯỚNG DẪN NHẬP DỮ LIỆU";
        guideSheet.Cell("A2").Value = "Điền dữ liệu theo từng sheet. Không đổi tên cột ở hàng 1.";
        guideSheet.Cell("A3").Value = "Các sheet có thể để trống nếu chưa có dữ liệu tương ứng.";
        guideSheet.Cell("A4").Value = "Kỳ hóa đơn dùng định dạng yyyy-MM, ví dụ: 2026-06.";
        guideSheet.Range("A1:A4").Style.Alignment.WrapText = true;
        guideSheet.Column("A").Width = 80;
        guideSheet.Row(1).Style.Font.Bold = true;
        guideSheet.Row(1).Style.Font.FontSize = 16;

        AddTemplateSheet(workbook, "Tòa nhà", ["Tên tòa nhà", "Chủ trọ", "Địa chỉ", "Mô tả"]);
        AddTemplateSheet(workbook, "Phòng trọ", ["Tên tòa nhà", "Tên phòng", "Trạng thái", "Giá phòng", "Giá điện", "Giá nước", "Diện tích", "Số người tối đa", "Mô tả"]);
        AddTemplateSheet(workbook, "Khách thuê", ["Họ tên", "Số điện thoại", "Email", "CCCD", "Ngày sinh", "Giới tính", "Nghề nghiệp", "Nơi làm việc", "Địa chỉ", "Ngày vào ở", "Ngày rời đi", "Trạng thái", "Ghi chú"]);
        AddTemplateSheet(workbook, "Hợp đồng", ["Tên phòng", "Họ tên khách", "Ngày bắt đầu", "Ngày kết thúc", "Giá thuê", "Tiền cọc", "Chu kỳ thanh toán", "Trạng thái cọc", "Hoàn cọc", "Khấu trừ cọc", "Trạng thái", "Ngày chấm dứt", "Lý do chấm dứt", "Ghi chú"]);
        AddTemplateSheet(workbook, "Phương tiện", ["Biển số", "Loại xe", "Hãng xe", "Màu sắc", "Họ tên khách", "Tên phòng", "Phí gửi xe", "Trạng thái", "Ngày đăng ký", "Ghi chú"]);
        AddTemplateSheet(workbook, "Danh mục thiết bị", ["Tên thiết bị", "Icon"]);
        AddTemplateSheet(workbook, "Thiết bị phòng", ["Tên phòng", "Tên thiết bị", "Danh mục thiết bị", "Số lượng", "Trạng thái", "Ảnh"]);
        AddTemplateSheet(workbook, "Dịch vụ", ["Tên dịch vụ", "Đơn giá", "Chu kỳ tính", "Đơn vị", "Icon"]);
        AddTemplateSheet(workbook, "Dịch vụ phòng", ["Tên phòng", "Tên dịch vụ"]);
        AddTemplateSheet(workbook, "Hóa đơn", ["Tên phòng", "Kỳ hóa đơn", "Tiền phòng", "Tiền điện", "Tiền nước", "Tiền dịch vụ", "Tiền gửi xe", "Khoản khác", "Giảm trừ", "Tổng tiền", "Hạn thanh toán", "Trạng thái", "Ngày thanh toán", "Số tiền đã thu", "Ghi chú"]);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    private static void AddTemplateSheet(XLWorkbook workbook, string sheetName, string[] headers)
    {
        var worksheet = workbook.Worksheets.Add(sheetName);
        AddHeader(worksheet, headers);
        FormatWorksheet(worksheet, headers.Select(_ => 18d).ToArray());
    }

    private async Task ImportBuildingAsync(IXLWorksheet? worksheet, Building building, CancellationToken cancellationToken)
    {
        if (worksheet == null)
        {
            return;
        }

        var headerMap = BuildHeaderMap(worksheet);
        var firstRow = GetDataRows(worksheet).FirstOrDefault();
        if (firstRow == null)
        {
            return;
        }

        var buildingName = GetString(firstRow, headerMap, "BuildingName");
        if (!string.IsNullOrWhiteSpace(buildingName))
        {
            building.BuildingName = buildingName.Trim();
        }

        building.Address = NullIfEmpty(GetString(firstRow, headerMap, "Address")) ?? building.Address;
        building.Description = NullIfEmpty(GetString(firstRow, headerMap, "Description"));
        await _excelImportRepository.SaveChangesAsync(cancellationToken);
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
                await _excelImportRepository.AddRoomAsync(room, cancellationToken);
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

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
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

        var tenants = await _excelImportRepository.GetTenantsWithContractsAsync(cancellationToken);

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
                await _excelImportRepository.AddTenantAsync(tenant, cancellationToken);
                tenants.Add(tenant);
            }

            tenant.FullName = fullName.Trim();
            tenant.PhoneNumber = NullIfEmpty(phone);
            tenant.Email = NullIfEmpty(email);
            tenant.CCCD = NullIfEmpty(cccd);
            tenant.Address = NullIfEmpty(GetString(row, headerMap, "Address"));
            tenant.DateOfBirth = GetNullableDate(row, headerMap, "DateOfBirth");
            tenant.Gender = NullIfEmpty(GetString(row, headerMap, "Gender"));
            tenant.Occupation = NullIfEmpty(GetString(row, headerMap, "Occupation"));
            tenant.Workplace = NullIfEmpty(GetString(row, headerMap, "Workplace"));
            tenant.MoveInDate = GetNullableDate(row, headerMap, "MoveInDate");
            tenant.MoveOutDate = GetNullableDate(row, headerMap, "MoveOutDate");
            tenant.IsActive = NormalizeTenantStatus(GetString(row, headerMap, "Status"));
            tenant.Note = NullIfEmpty(GetString(row, headerMap, "Note"));
            tenant.UpdatedAt = DateTime.UtcNow;

            imported++;
        }

        await _excelImportRepository.SaveChangesAsync(cancellationToken);

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

            var contract = await _excelImportRepository.GetActiveContractAsync(room.RoomId, tenant.TenantId, cancellationToken);

            if (contract == null)
            {
                contract = new Contract
                {
                    RoomId = room.RoomId,
                    TenantId = tenant.TenantId,
                };
                await _excelImportRepository.AddContractAsync(contract, cancellationToken);
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

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task<int> ImportContractsAsync(
        IXLWorksheet? worksheet,
        Dictionary<string, Room> roomsByName,
        List<Tenant> tenants,
        List<string> warnings,
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
            var tenantName = GetString(row, headerMap, "FullName");

            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(tenantName))
            {
                continue;
            }

            if (!roomsByName.TryGetValue(NormalizeKey(roomName), out var room))
            {
                warnings.Add($"Khong tim thay phong '{roomName}' de nhap hop dong.");
                continue;
            }

            var tenant = FindTenant(tenants, tenantName);
            if (tenant == null)
            {
                warnings.Add($"Khong tim thay khach '{tenantName}' de nhap hop dong phong '{roomName}'.");
                continue;
            }

            var startDate = GetNullableDate(row, headerMap, "ContractStartDate") ?? DateTime.Today;
            var endDate = GetNullableDate(row, headerMap, "ContractEndDate") ?? startDate.AddMonths(12);
            var contract = await _excelImportRepository.GetActiveContractAsync(room.RoomId, tenant.TenantId, cancellationToken);

            if (contract == null)
            {
                contract = new Contract
                {
                    RoomId = room.RoomId,
                    TenantId = tenant.TenantId,
                };
                await _excelImportRepository.AddContractAsync(contract, cancellationToken);
                imported++;
            }

            contract.StartDate = startDate;
            contract.EndDate = endDate;
            contract.RentPrice = GetDecimal(row, headerMap, "RentPrice");
            contract.Deposit = GetDecimal(row, headerMap, "Deposit");
            contract.PaymentCycle = NullIfEmpty(GetString(row, headerMap, "BillingCycle")) ?? "Monthly";
            contract.DepositStatus = NullIfEmpty(GetString(row, headerMap, "DepositStatus")) ?? contract.DepositStatus;
            contract.Status = NullIfEmpty(GetString(row, headerMap, "Status")) ?? "Active";
            contract.Note = NullIfEmpty(GetString(row, headerMap, "Note"));

            room.Status = "occupied";
            room.UpdatedAt = DateTime.UtcNow;
        }

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task<int> ImportVehiclesAsync(
        IXLWorksheet? worksheet,
        Dictionary<string, Room> roomsByName,
        List<Tenant> tenants,
        List<string> warnings,
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
            var plate = GetString(row, headerMap, "LicensePlateNumber");
            if (string.IsNullOrWhiteSpace(plate))
            {
                continue;
            }

            var vehicle = await _excelImportRepository.GetVehicleByLicensePlateAsync(plate.Trim(), cancellationToken);
            if (vehicle == null)
            {
                vehicle = new Vehicle { LicensePlateNumber = plate.Trim() };
                await _excelImportRepository.AddVehicleAsync(vehicle, cancellationToken);
                imported++;
            }

            var roomName = GetString(row, headerMap, "RoomName");
            if (!string.IsNullOrWhiteSpace(roomName))
            {
                if (roomsByName.TryGetValue(NormalizeKey(roomName), out var room))
                {
                    vehicle.RoomId = room.RoomId;
                }
                else
                {
                    warnings.Add($"Khong tim thay phong '{roomName}' de nhap phuong tien '{plate}'.");
                }
            }

            var tenantName = GetString(row, headerMap, "FullName");
            if (!string.IsNullOrWhiteSpace(tenantName))
            {
                var tenant = FindTenant(tenants, tenantName);
                if (tenant != null)
                {
                    vehicle.TenantId = tenant.TenantId;
                }
                else
                {
                    warnings.Add($"Khong tim thay khach '{tenantName}' de nhap phuong tien '{plate}'.");
                }
            }

            vehicle.VehicleType = NullIfEmpty(GetString(row, headerMap, "VehicleType"));
            vehicle.Brand = NullIfEmpty(GetString(row, headerMap, "Brand"));
            vehicle.Color = NullIfEmpty(GetString(row, headerMap, "Color"));
            vehicle.ParkingFee = GetDecimal(row, headerMap, "ParkingFee");
            vehicle.Status = NullIfEmpty(GetString(row, headerMap, "Status")) ?? "active";
            vehicle.RegistrationDate = GetNullableDate(row, headerMap, "RegistrationDate");
            vehicle.Notes = NullIfEmpty(GetString(row, headerMap, "Notes"));
            vehicle.UpdatedAt = DateTime.UtcNow;
        }

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task ImportDeviceCatalogsAsync(IXLWorksheet? worksheet, CancellationToken cancellationToken)
    {
        if (worksheet == null)
        {
            return;
        }

        var headerMap = BuildHeaderMap(worksheet);
        foreach (var row in GetDataRows(worksheet))
        {
            var name = GetString(row, headerMap, "DeviceName");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var catalog = await _excelImportRepository.GetDeviceCatalogByNameAsync(name.Trim(), cancellationToken);
            if (catalog == null)
            {
                catalog = new DeviceCatalog { Name = name.Trim() };
                await _excelImportRepository.AddDeviceCatalogAsync(catalog, cancellationToken);
            }

            catalog.Icon = NullIfEmpty(GetString(row, headerMap, "Icon"));
        }

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> ImportDevicesAsync(
        IXLWorksheet? worksheet,
        Dictionary<string, Room> roomsByName,
        List<string> warnings,
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
            var deviceName = GetString(row, headerMap, "DeviceName");
            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(deviceName))
            {
                continue;
            }

            if (!roomsByName.TryGetValue(NormalizeKey(roomName), out var room))
            {
                warnings.Add($"Khong tim thay phong '{roomName}' de nhap thiet bi '{deviceName}'.");
                continue;
            }

            var device = await _excelImportRepository.GetDeviceAsync(room.RoomId, deviceName.Trim(), cancellationToken);
            if (device == null)
            {
                device = new Device
                {
                    RoomId = room.RoomId,
                    DeviceName = deviceName.Trim(),
                };
                await _excelImportRepository.AddDeviceAsync(device, cancellationToken);
                imported++;
            }

            var catalogName = GetString(row, headerMap, "DeviceCatalogName");
            if (!string.IsNullOrWhiteSpace(catalogName))
            {
                var catalog = await _excelImportRepository.GetDeviceCatalogByNameAsync(catalogName.Trim(), cancellationToken);
                if (catalog == null)
                {
                    catalog = new DeviceCatalog { Name = catalogName.Trim() };
                    await _excelImportRepository.AddDeviceCatalogAsync(catalog, cancellationToken);
                    await _excelImportRepository.SaveChangesAsync(cancellationToken);
                }

                device.DeviceCatalogId = catalog.DeviceCatalogId;
            }

            device.Quantity = GetNullableInt(row, headerMap, "Quantity") ?? 1;
            device.Status = NullIfEmpty(GetString(row, headerMap, "Status")) ?? "Working";
            device.ImageUrl = NullIfEmpty(GetString(row, headerMap, "ImageUrl"));
        }

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task<int> ImportServicesAsync(IXLWorksheet? worksheet, CancellationToken cancellationToken)
    {
        if (worksheet == null)
        {
            return 0;
        }

        var headerMap = BuildHeaderMap(worksheet);
        var imported = 0;

        foreach (var row in GetDataRows(worksheet))
        {
            var serviceName = GetString(row, headerMap, "ServiceName");
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                continue;
            }

            var service = await _excelImportRepository.GetServiceByNameAsync(serviceName.Trim(), cancellationToken);
            if (service == null)
            {
                service = new Service { ServiceName = serviceName.Trim() };
                await _excelImportRepository.AddServiceAsync(service, cancellationToken);
                imported++;
            }

            service.UnitPrice = GetDecimal(row, headerMap, "UnitPrice");
            service.BillingCycle = NullIfEmpty(GetString(row, headerMap, "BillingCycle")) ?? "Monthly";
            service.Unit = NullIfEmpty(GetString(row, headerMap, "Unit"));
            service.Icon = NullIfEmpty(GetString(row, headerMap, "Icon"));
        }

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task ImportRoomServicesAsync(
        IXLWorksheet? worksheet,
        Dictionary<string, Room> roomsByName,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        if (worksheet == null)
        {
            return;
        }

        var headerMap = BuildHeaderMap(worksheet);
        foreach (var row in GetDataRows(worksheet))
        {
            var roomName = GetString(row, headerMap, "RoomName");
            var serviceName = GetString(row, headerMap, "ServiceName");
            if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(serviceName))
            {
                continue;
            }

            if (!roomsByName.TryGetValue(NormalizeKey(roomName), out var room))
            {
                warnings.Add($"Khong tim thay phong '{roomName}' de gan dich vu '{serviceName}'.");
                continue;
            }

            var service = await _excelImportRepository.GetServiceByNameAsync(serviceName.Trim(), cancellationToken);
            if (service == null)
            {
                service = new Service { ServiceName = serviceName.Trim(), BillingCycle = "Monthly" };
                await _excelImportRepository.AddServiceAsync(service, cancellationToken);
                await _excelImportRepository.SaveChangesAsync(cancellationToken);
            }

            var roomService = await _excelImportRepository.GetRoomServiceAsync(room.RoomId, service.ServiceId, cancellationToken);
            if (roomService == null)
            {
                await _excelImportRepository.AddRoomServiceAsync(new RoomService
                {
                    RoomId = room.RoomId,
                    ServiceId = service.ServiceId,
                }, cancellationToken);
            }
        }

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
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
        var invoices = await _excelImportRepository.GetInvoicesWithPaymentsAsync(roomIds, cancellationToken);

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
                await _excelImportRepository.AddInvoiceAsync(invoice, cancellationToken);
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
                _excelImportRepository.RemovePayments(invoice.Payments);
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

        await _excelImportRepository.SaveChangesAsync(cancellationToken);
        return imported;
    }

    private async Task<User> EnsureDefaultOwnerAsync(CancellationToken cancellationToken)
    {
        var ownerRole = await _excelImportRepository.GetRoleByNameAsync("Owner", cancellationToken);
        if (ownerRole == null)
        {
            ownerRole = new Role
            {
                Name = "Owner",
                Description = "Chu tro"
            };
            await _excelImportRepository.AddRoleAsync(ownerRole, cancellationToken);
            await _excelImportRepository.SaveChangesAsync(cancellationToken);
        }

        var owner = await _excelImportRepository.GetUserByRoleIdAsync(ownerRole.RoleId, cancellationToken);
        if (owner != null)
        {
            return owner;
        }

        throw new InvalidOperationException("Vui lòng tạo tài khoản chủ trọ trước khi nhập Excel.");
    }

    private async Task<Building> EnsureDefaultBuildingAsync(User owner, CancellationToken cancellationToken)
    {
        var building = await _excelImportRepository.GetBuildingByUserIdAsync(owner.UserId, cancellationToken);
        if (building != null)
        {
            return building;
        }

        throw new InvalidOperationException("Vui lòng tạo tòa nhà trước khi nhập Excel.");
    }

    private static Tenant? FindTenant(IEnumerable<Tenant> tenants, string fullName)
    {
        var normalizedName = NormalizeKey(fullName);
        return tenants.FirstOrDefault(tenant => NormalizeKey(tenant.FullName) == normalizedName);
    }

    private static IXLWorksheet? FindWorksheet(XLWorkbook workbook, params string[] possibleNames)
    {
        var possibleKeys = possibleNames
            .Select(NormalizeKey)
            .SelectMany(ExpandWorksheetAliases)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return workbook.Worksheets.FirstOrDefault(sheet =>
            possibleKeys.Contains(NormalizeKey(sheet.Name)));
    }

    private static IEnumerable<string> ExpandWorksheetAliases(string normalizedName)
    {
        yield return normalizedName;

        foreach (var alias in normalizedName switch
        {
            "rooms" or "phong" => ["phongtro"],
            "invoices" or "hoadon" => ["hoadon"],
            "contracts" or "hopdong" => ["hopdong"],
            "vehicles" or "phuongtien" => ["phuongtien"],
            "devices" or "thietbiphong" => ["thietbiphong"],
            "services" or "dichvu" => ["dichvu"],
            "roomservices" or "dichvuphong" => ["dichvuphong"],
            "devicecatalogs" or "danhmucthietbi" => ["danhmucthietbi"],
            "buildings" or "toanha" => ["toanha"],
            _ => Array.Empty<string>(),
        })
        {
            yield return alias;
        }
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
