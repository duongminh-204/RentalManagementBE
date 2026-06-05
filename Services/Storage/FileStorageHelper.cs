namespace Backend.Services.Storage;

internal static class FileStorageHelper
{
    internal const string UploadsPrefix = "uploads";

    internal static string ToPublicPath(string folder, string fileName) =>
        $"/{UploadsPrefix}/{folder.Trim('/')}/{fileName}";

    internal static string ToBlobName(string folder, string fileName) =>
        $"{UploadsPrefix}/{folder.Trim('/')}/{fileName}";

    internal static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }

    internal static string? ResolveBlobName(string storedPathOrUrl)
    {
        if (Uri.TryCreate(storedPathOrUrl, UriKind.Absolute, out var uri))
        {
            var path = uri.LocalPath.TrimStart('/');
            var uploadsIndex = path.IndexOf($"{UploadsPrefix}/", StringComparison.OrdinalIgnoreCase);
            if (uploadsIndex >= 0)
                return path[uploadsIndex..];
        }

        var normalized = storedPathOrUrl.TrimStart('/').Replace('\\', '/');
        return normalized.StartsWith($"{UploadsPrefix}/", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : null;
    }
}
