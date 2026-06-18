using Backend.DTOs.Rooms;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Backend.Services;

public class RoomDecorService : IRoomDecorService
{
    private static readonly IReadOnlyList<RoomDecorStyleDto> Styles =
    [
        new()
        {
            Id = "birthday",
            Label = "Sinh nhật",
            Description = "Bóng bay, hoa và đèn fairy — phù hợp tiệc sinh nhật",
            Prompt =
                "professional event decoration added to this exact room, pink birthday decor, balloon arch, " +
                "flower backdrop, fairy lights, keep original walls floor ceiling windows unchanged, " +
                "photorealistic, masterpiece, highly detailed, 8k"
        },
        new()
        {
            Id = "modern",
            Label = "Hiện đại tối giản",
            Description = "Nội thất gọn gàng, tông trung tính, ánh sáng tự nhiên",
            Prompt =
                "modern minimalist room decoration, clean furniture arrangement, neutral tones, indoor plants, " +
                "soft natural lighting, keep original walls floor ceiling windows unchanged, " +
                "photorealistic, interior design, highly detailed, 8k"
        },
        new()
        {
            Id = "cozy",
            Label = "Ấm cúng",
            Description = "Thảm, gối, đèn ấm — không gian thư giãn",
            Prompt =
                "cozy warm room decoration, soft rug, warm string lights, cushions, small bookshelf, " +
                "keep original walls floor ceiling windows unchanged, photorealistic, inviting atmosphere, 8k"
        },
        new()
        {
            Id = "japanese",
            Label = "Phong cách Nhật",
            Description = "Tối giản Zen, gỗ tự nhiên, cây xanh nhỏ",
            Prompt =
                "japanese zen room decoration, tatami mat accents, low wooden table, bonsai plant, shoji screen, " +
                "keep original walls floor ceiling windows unchanged, photorealistic, serene, highly detailed, 8k"
        },
        new()
        {
            Id = "student",
            Label = "Phòng sinh viên",
            Description = "Bàn học, kệ sách, decor gọn cho phòng trọ",
            Prompt =
                "affordable student room decoration, study desk setup, wall shelves, motivational poster, " +
                "small plant, keep original walls floor ceiling windows unchanged, photorealistic, tidy, 8k"
        },
        new()
        {
            Id = "luxury",
            Label = "Cao cấp",
            Description = "Chandelier, rèm sang, nội thất premium",
            Prompt =
                "luxury room decoration, elegant curtains, premium furniture, chandelier, marble accents, " +
                "keep original walls floor ceiling windows unchanged, photorealistic, upscale interior, 8k"
        }
    ];

    private readonly IComfyUIService _comfyUI;
    private readonly IFileStorageService _fileStorage;
    private readonly IRoomManagementRepository _roomRepo;
    private readonly IConfiguration _configuration;

    public RoomDecorService(
        IComfyUIService comfyUI,
        IFileStorageService fileStorage,
        IRoomManagementRepository roomRepo,
        IConfiguration configuration)
    {
        _comfyUI = comfyUI;
        _fileStorage = fileStorage;
        _roomRepo = roomRepo;
        _configuration = configuration;
    }

    public IReadOnlyList<RoomDecorStyleDto> GetStyles() => Styles;

    public async Task<RoomDecorStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["ComfyUI:BaseUrl"] ?? "http://127.0.0.1:8188";
        var available = await _comfyUI.IsAvailableAsync(cancellationToken);

        return new RoomDecorStatusDto
        {
            IsAvailable = available,
            BaseUrl = baseUrl,
            Message = available
                ? "ComfyUI đang sẵn sàng."
                : "Không kết nối được ComfyUI. Hãy chạy server AI và kiểm tra ComfyUI:BaseUrl trong appsettings."
        };
    }

    public async Task<RoomDecorResultDto> GenerateAsync(
        IFormFile file,
        string? styleId,
        string? customPrompt,
        int? roomId,
        bool saveToRoom,
        CancellationToken cancellationToken = default)
    {
        ValidateImageFile(file);

        if (saveToRoom || roomId.HasValue)
        {
            if (!roomId.HasValue)
                throw new InvalidOperationException("Cần chọn phòng khi lưu ảnh decor.");

            if (!await _roomRepo.RoomExistsAsync(roomId.Value))
                throw new KeyNotFoundException("Không tìm thấy phòng.");
        }

        var positivePrompt = ResolvePrompt(styleId, customPrompt);

        await using var stream = file.OpenReadStream();
        var (imageBytes, promptId, durationMs) = await _comfyUI.GenerateDecorImageAsync(
            stream,
            file.FileName,
            positivePrompt,
            cancellationToken);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            ext = ".png";

        var fileName = $"decor_{Guid.NewGuid():N}{ext}";
        await _fileStorage.SaveBytesAsync(imageBytes, "rooms", fileName, cancellationToken);
        var imageUrl = $"/uploads/rooms/{fileName}";

        var result = new RoomDecorResultDto
        {
            ImageUrl = imageUrl,
            PromptId = promptId,
            DurationMs = durationMs,
            SavedToRoom = false
        };

        if (saveToRoom && roomId.HasValue)
        {
            var roomImage = new RoomImage
            {
                RoomId = roomId.Value,
                ImageUrl = imageUrl
            };
            _roomRepo.AddRoomImage(roomImage);
            await _roomRepo.SaveChangesAsync(cancellationToken);

            result.SavedToRoom = true;
            result.RoomImageId = roomImage.RoomImageId;
        }

        return result;
    }

    private static void ValidateImageFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("File ảnh không hợp lệ.");

        if (file.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("Ảnh tối đa 10MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG, WEBP.");
    }

    private string ResolvePrompt(string? styleId, string? customPrompt)
    {
        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            var trimmed = customPrompt.Trim();
            if (trimmed.Length > 2000)
                throw new InvalidOperationException("Prompt tối đa 2000 ký tự.");

            return trimmed;
        }

        if (string.IsNullOrWhiteSpace(styleId))
            throw new InvalidOperationException("Vui lòng chọn phong cách decor hoặc nhập prompt tùy chỉnh.");

        var style = Styles.FirstOrDefault(s =>
            string.Equals(s.Id, styleId, StringComparison.OrdinalIgnoreCase));

        if (style == null)
            throw new InvalidOperationException("Phong cách decor không hợp lệ.");

        return style.Prompt;
    }
}
