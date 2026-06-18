namespace Backend.Configuration;

public class ComfyUIOptions
{
    public const string SectionName = "ComfyUI";

    /// <summary>URL ComfyUI server, ví dụ http://127.0.0.1:8188</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:8188";

    public string Checkpoint { get; set; } = "RealVisXL_V5.0_fp16.safetensors";

    public int Steps { get; set; } = 25;

    public float Cfg { get; set; } = 7f;

    public float Denoise { get; set; } = 0.58f;

    public string SamplerName { get; set; } = "euler";

    public string Scheduler { get; set; } = "normal";

    /// <summary>Thời gian chờ tối đa (giây) khi poll kết quả từ ComfyUI.</summary>
    public int PollTimeoutSeconds { get; set; } = 180;

    public int PollIntervalMs { get; set; } = 1500;
}
