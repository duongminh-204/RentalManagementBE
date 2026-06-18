namespace Backend.Services.Interfaces;

public interface IComfyUIService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    Task<(byte[] ImageBytes, string PromptId, int DurationMs)> GenerateDecorImageAsync(
        Stream imageStream,
        string fileName,
        string positivePrompt,
        CancellationToken cancellationToken = default);
}
