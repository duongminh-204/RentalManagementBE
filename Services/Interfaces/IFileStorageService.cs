namespace Backend.Services.Interfaces;

public record FileDownloadResult(Stream Stream, string ContentType, string FileName);

public interface IFileStorageService
{
    Task<string> UploadFormFileAsync(IFormFile file, string folder, string fileName, CancellationToken cancellationToken = default);
    Task<string> UploadTextAsync(string content, string folder, string fileName, CancellationToken cancellationToken = default);
    Task SaveBytesAsync(byte[] content, string folder, string fileName, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadBytesAsync(string folder, string fileName, CancellationToken cancellationToken = default);
    Task<string> ReadTextAsync(string folder, string fileName, CancellationToken cancellationToken = default);
    Task EnsureTextFileAsync(string folder, string fileName, string defaultContent, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? storedPathOrUrl, CancellationToken cancellationToken = default);
    Task<FileDownloadResult?> OpenReadAsync(string storedPathOrUrl, CancellationToken cancellationToken = default);
    bool IsRemoteUrl(string? pathOrUrl);
}
