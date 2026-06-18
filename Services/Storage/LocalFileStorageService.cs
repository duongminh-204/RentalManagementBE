using Backend.Services.Interfaces;

namespace Backend.Services.Storage;

/// <summary>
/// Dev local: lưu file vào wwwroot/uploads (giữ nguyên logic cũ).
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly string _webRoot;

    public LocalFileStorageService(IWebHostEnvironment env)
    {
        _webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    }

    public bool IsRemoteUrl(string? pathOrUrl) =>
        !string.IsNullOrWhiteSpace(pathOrUrl) &&
        Uri.TryCreate(pathOrUrl, UriKind.Absolute, out _);

    public async Task<string> UploadFormFileAsync(IFormFile file, string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var uploadsDir = Path.Combine(_webRoot, FileStorageHelper.UploadsPrefix, folder.Trim('/'));
        Directory.CreateDirectory(uploadsDir);

        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return FileStorageHelper.ToPublicPath(folder, fileName);
    }

    public async Task<string> UploadTextAsync(string content, string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var uploadsDir = Path.Combine(_webRoot, FileStorageHelper.UploadsPrefix, folder.Trim('/'));
        Directory.CreateDirectory(uploadsDir);

        var filePath = Path.Combine(uploadsDir, fileName);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
        return FileStorageHelper.ToPublicPath(folder, fileName);
    }

    public async Task<string> SaveBytesAsync(byte[] content, string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var uploadsDir = Path.Combine(_webRoot, FileStorageHelper.UploadsPrefix, folder.Trim('/'));
        Directory.CreateDirectory(uploadsDir);
        await File.WriteAllBytesAsync(Path.Combine(uploadsDir, fileName), content, cancellationToken);
        return FileStorageHelper.ToPublicPath(folder, fileName);
    }

    public async Task<byte[]?> ReadBytesAsync(string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = GetPhysicalPath(folder, fileName);
        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllBytesAsync(filePath, cancellationToken);
    }

    public async Task<string> ReadTextAsync(string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = GetPhysicalPath(folder, fileName);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Không tìm thấy file {fileName}.");

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    public async Task EnsureTextFileAsync(string folder, string fileName, string defaultContent, CancellationToken cancellationToken = default)
    {
        var filePath = GetPhysicalPath(folder, fileName);
        if (File.Exists(filePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllTextAsync(filePath, defaultContent, cancellationToken);
    }

    public Task DeleteAsync(string? storedPathOrUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPathOrUrl) || IsRemoteUrl(storedPathOrUrl))
            return Task.CompletedTask;

        try
        {
            var normalized = storedPathOrUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(_webRoot, normalized));

            if (!fullPath.StartsWith(Path.GetFullPath(_webRoot), StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
            // Bỏ qua lỗi khi dọn file cũ
        }

        return Task.CompletedTask;
    }

    public Task<FileDownloadResult?> OpenReadAsync(string storedPathOrUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPathOrUrl) || IsRemoteUrl(storedPathOrUrl))
            return Task.FromResult<FileDownloadResult?>(null);

        var normalized = storedPathOrUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_webRoot, normalized));

        if (!fullPath.StartsWith(Path.GetFullPath(_webRoot), StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            return Task.FromResult<FileDownloadResult?>(null);

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = Path.GetFileName(fullPath);
        return Task.FromResult<FileDownloadResult?>(new FileDownloadResult(stream, FileStorageHelper.GetContentType(fileName), fileName));
    }

    private string GetPhysicalPath(string folder, string fileName) =>
        Path.Combine(_webRoot, FileStorageHelper.UploadsPrefix, folder.Trim('/'), fileName);
}
