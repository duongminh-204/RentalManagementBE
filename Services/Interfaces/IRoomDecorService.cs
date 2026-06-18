using Backend.DTOs.Rooms;
using Microsoft.AspNetCore.Http;

namespace Backend.Services.Interfaces;

public interface IRoomDecorService
{
    IReadOnlyList<RoomDecorStyleDto> GetStyles();

    Task<RoomDecorStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<RoomDecorResultDto> GenerateAsync(
        IFormFile file,
        string? styleId,
        string? customPrompt,
        int? roomId,
        bool saveToRoom,
        CancellationToken cancellationToken = default);
}
