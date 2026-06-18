using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Configuration;
using Backend.Services.ComfyUI;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Backend.Services;

public class ComfyUIService : IComfyUIService
{
    private readonly HttpClient _httpClient;
    private readonly ComfyUIOptions _options;
    private readonly ILogger<ComfyUIService> _logger;

    public ComfyUIService(HttpClient httpClient, IOptions<ComfyUIOptions> options, ILogger<ComfyUIService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ComfyUI không phản hồi tại {BaseUrl}", _options.BaseUrl);
            return false;
        }
    }

    public async Task<(byte[] ImageBytes, string PromptId, int DurationMs)> GenerateDecorImageAsync(
        Stream imageStream,
        string fileName,
        string positivePrompt,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var uploadedName = await UploadImageAsync(imageStream, fileName, cancellationToken);
        var workflow = ComfyUIWorkflowBuilder.BuildImg2ImgWorkflow(uploadedName, positivePrompt, _options);
        var promptId = await QueuePromptAsync(workflow, cancellationToken);
        var output = await WaitForOutputAsync(promptId, cancellationToken);
        var imageBytes = await DownloadImageAsync(output.Filename, output.Subfolder, output.Type, cancellationToken);
        sw.Stop();
        return (imageBytes, promptId, (int)sw.ElapsedMilliseconds);
    }

    private async Task<string> UploadImageAsync(Stream stream, string fileName, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "image", fileName);
        content.Add(new StringContent("true"), "overwrite");

        using var response = await _httpClient.PostAsync("upload/image", content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ComfyUI upload thất bại: {body}");

        using var doc = JsonDocument.Parse(body);
        var name = doc.RootElement.GetProperty("name").GetString();
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("ComfyUI không trả về tên file sau khi upload.");

        return name;
    }

    private async Task<string> QueuePromptAsync(Dictionary<string, object> workflow, CancellationToken cancellationToken)
    {
        var payload = new
        {
            prompt = workflow,
            client_id = Guid.NewGuid().ToString("N")
        };

        using var response = await _httpClient.PostAsJsonAsync("prompt", payload, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"ComfyUI queue thất bại: {body}");

        using var doc = JsonDocument.Parse(body);
        var promptId = doc.RootElement.GetProperty("prompt_id").GetString();
        if (string.IsNullOrWhiteSpace(promptId))
            throw new InvalidOperationException("ComfyUI không trả về prompt_id.");

        return promptId;
    }

    private async Task<(string Filename, string Subfolder, string Type)> WaitForOutputAsync(
        string promptId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(_options.PollTimeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var response = await _httpClient.GetAsync($"history/{promptId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                await Task.Delay(_options.PollIntervalMs, cancellationToken);
                continue;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(body);

            if (!doc.RootElement.TryGetProperty(promptId, out var entry))
            {
                await Task.Delay(_options.PollIntervalMs, cancellationToken);
                continue;
            }

            if (entry.TryGetProperty("status", out var statusNode) &&
                statusNode.TryGetProperty("status_str", out var statusStr) &&
                statusStr.GetString() is "error")
            {
                var messages = statusNode.TryGetProperty("messages", out var msgs)
                    ? msgs.ToString()
                    : "unknown error";
                throw new InvalidOperationException($"ComfyUI xử lý lỗi: {messages}");
            }

            if (!entry.TryGetProperty("outputs", out var outputs))
            {
                await Task.Delay(_options.PollIntervalMs, cancellationToken);
                continue;
            }

            foreach (var outputNode in outputs.EnumerateObject())
            {
                if (!outputNode.Value.TryGetProperty("images", out var images))
                    continue;

                foreach (var image in images.EnumerateArray())
                {
                    var filename = image.GetProperty("filename").GetString();
                    if (string.IsNullOrWhiteSpace(filename))
                        continue;

                    var subfolder = image.TryGetProperty("subfolder", out var sf)
                        ? sf.GetString() ?? string.Empty
                        : string.Empty;
                    var type = image.TryGetProperty("type", out var tp)
                        ? tp.GetString() ?? "output"
                        : "output";

                    return (filename, subfolder, type);
                }
            }

            await Task.Delay(_options.PollIntervalMs, cancellationToken);
        }

        throw new TimeoutException(
            $"ComfyUI không trả kết quả trong {_options.PollTimeoutSeconds} giây. Hãy kiểm tra server và model checkpoint.");
    }

    private async Task<byte[]> DownloadImageAsync(
        string filename,
        string subfolder,
        string type,
        CancellationToken cancellationToken)
    {
        var query = $"view?filename={Uri.EscapeDataString(filename)}&type={Uri.EscapeDataString(type)}";
        if (!string.IsNullOrEmpty(subfolder))
            query += $"&subfolder={Uri.EscapeDataString(subfolder)}";

        using var response = await _httpClient.GetAsync(query, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Không tải được ảnh kết quả từ ComfyUI.");

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
