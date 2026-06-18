using Backend.Configuration;

namespace Backend.Services.ComfyUI;

/// <summary>
/// Sinh workflow img2img tương thích BloomyAI-img2img cho ComfyUI API.
/// </summary>
public static class ComfyUIWorkflowBuilder
{
    private const string DefaultNegativePrompt =
        "blurry, low quality, distorted, ugly, deformed, watermark, text, bad anatomy, extra fingers, " +
        "changed room structure, different walls, different floor, warped architecture";

    public static Dictionary<string, object> BuildImg2ImgWorkflow(
        string uploadedImageName,
        string positivePrompt,
        ComfyUIOptions options,
        long? seed = null)
    {
        var resolvedSeed = seed ?? Random.Shared.NextInt64(0, 1_000_000_000_000_000);

        return new Dictionary<string, object>
        {
            ["1"] = new Dictionary<string, object>
            {
                ["class_type"] = "LoadImage",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["image"] = uploadedImageName
                }
            },
            ["2"] = new Dictionary<string, object>
            {
                ["class_type"] = "CheckpointLoaderSimple",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["ckpt_name"] = options.Checkpoint
                }
            },
            ["10"] = new Dictionary<string, object>
            {
                ["class_type"] = "CLIPTextEncode",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["clip"] = new object[] { "2", 1 },
                    ["text"] = positivePrompt
                }
            },
            ["3"] = new Dictionary<string, object>
            {
                ["class_type"] = "CLIPTextEncode",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["clip"] = new object[] { "2", 1 },
                    ["text"] = DefaultNegativePrompt
                }
            },
            ["11"] = new Dictionary<string, object>
            {
                ["class_type"] = "VAEEncode",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["pixels"] = new object[] { "1", 0 },
                    ["vae"] = new object[] { "2", 2 }
                }
            },
            ["7"] = new Dictionary<string, object>
            {
                ["class_type"] = "KSampler",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["model"] = new object[] { "2", 0 },
                    ["positive"] = new object[] { "10", 0 },
                    ["negative"] = new object[] { "3", 0 },
                    ["latent_image"] = new object[] { "11", 0 },
                    ["seed"] = resolvedSeed,
                    ["steps"] = options.Steps,
                    ["cfg"] = options.Cfg,
                    ["sampler_name"] = options.SamplerName,
                    ["scheduler"] = options.Scheduler,
                    ["denoise"] = options.Denoise
                }
            },
            ["9"] = new Dictionary<string, object>
            {
                ["class_type"] = "VAEDecode",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["samples"] = new object[] { "7", 0 },
                    ["vae"] = new object[] { "2", 2 }
                }
            },
            ["4"] = new Dictionary<string, object>
            {
                ["class_type"] = "SaveImage",
                ["inputs"] = new Dictionary<string, object>
                {
                    ["images"] = new object[] { "9", 0 },
                    ["filename_prefix"] = "RoomDecor"
                }
            }
        };
    }
}
