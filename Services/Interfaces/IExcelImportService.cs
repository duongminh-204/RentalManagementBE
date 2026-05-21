using Backend.DTOs.Dashboard;

namespace Backend.Services.Interfaces;

public interface IExcelImportService
{
    Task<ExcelImportResultDto> ImportDashboardSeedAsync(IFormFile file, CancellationToken cancellationToken = default);
    byte[] GenerateTemplate();
    Task SaveTemplateFileAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<(byte[] Content, string FileName)> GetTemplateFileAsync(CancellationToken cancellationToken = default);
}
