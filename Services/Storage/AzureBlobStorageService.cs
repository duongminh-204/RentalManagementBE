using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Backend.Services.Storage;

public class AzureStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "rental-uploads";
}

/// <summary>
/// Production (Render): lưu file lên Azure Blob Storage.
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(IOptions<AzureStorageOptions> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("Azure Storage connection string is missing.");

        var serviceClient = new BlobServiceClient(settings.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(settings.ContainerName);
        _container.CreateIfNotExists(PublicAccessType.Blob);
    }

    public bool IsRemoteUrl(string? pathOrUrl) =>
        !string.IsNullOrWhiteSpace(pathOrUrl) &&
        Uri.TryCreate(pathOrUrl, UriKind.Absolute, out _);

    public async Task<string> UploadFormFileAsync(IFormFile file, string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(FileStorageHelper.ToBlobName(folder, fileName));
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, overwrite: true, cancellationToken);
        await blob.SetHttpHeadersAsync(new BlobHttpHeaders
        {
            ContentType = file.ContentType ?? FileStorageHelper.GetContentType(fileName)
        }, cancellationToken: cancellationToken);

        return blob.Uri.ToString();
    }

    public async Task<string> UploadTextAsync(string content, string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(FileStorageHelper.ToBlobName(folder, fileName));
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken);
        await blob.SetHttpHeadersAsync(new BlobHttpHeaders
        {
            ContentType = FileStorageHelper.GetContentType(fileName)
        }, cancellationToken: cancellationToken);

        return blob.Uri.ToString();
    }

    public async Task<string> SaveBytesAsync(byte[] content, string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(FileStorageHelper.ToBlobName(folder, fileName));
        await using var stream = new MemoryStream(content);
        await blob.UploadAsync(stream, overwrite: true, cancellationToken);
        await blob.SetHttpHeadersAsync(new BlobHttpHeaders
        {
            ContentType = FileStorageHelper.GetContentType(fileName)
        }, cancellationToken: cancellationToken);
        return blob.Uri.ToString();
    }

    public async Task<byte[]?> ReadBytesAsync(string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(FileStorageHelper.ToBlobName(folder, fileName));
        if (!await blob.ExistsAsync(cancellationToken))
            return null;

        var response = await blob.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToArray();
    }

    public async Task<string> ReadTextAsync(string folder, string fileName, CancellationToken cancellationToken = default)
    {
        var bytes = await ReadBytesAsync(folder, fileName, cancellationToken)
            ?? throw new FileNotFoundException($"Không tìm thấy file {fileName}.");

        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    public async Task EnsureTextFileAsync(string folder, string fileName, string defaultContent, CancellationToken cancellationToken = default)
    {
        if (await ExistsAsync(folder, fileName, cancellationToken))
            return;

        await UploadTextAsync(defaultContent, folder, fileName, cancellationToken);
    }

    public async Task DeleteAsync(string? storedPathOrUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPathOrUrl))
            return;

        var blobName = FileStorageHelper.ResolveBlobName(storedPathOrUrl);
        if (blobName == null)
            return;

        var blob = _container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }

    public async Task<FileDownloadResult?> OpenReadAsync(string storedPathOrUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPathOrUrl))
            return null;

        var blobName = FileStorageHelper.ResolveBlobName(storedPathOrUrl);
        if (blobName == null)
            return null;

        var blob = _container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken))
            return null;

        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        var fileName = Path.GetFileName(blobName);
        var contentType = response.Value.Details.ContentType ?? FileStorageHelper.GetContentType(fileName);
        return new FileDownloadResult(response.Value.Content, contentType, fileName);
    }

    private async Task<bool> ExistsAsync(string folder, string fileName, CancellationToken cancellationToken)
    {
        var blob = _container.GetBlobClient(FileStorageHelper.ToBlobName(folder, fileName));
        return await blob.ExistsAsync(cancellationToken);
    }
}
