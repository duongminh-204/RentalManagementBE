using Backend.DTOs.Rooms;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/room-management")]
[ApiController]
public class RoomManagementController : ControllerBase
{
    private readonly IRoomManagementService _management;

    public RoomManagementController(IRoomManagementService management)
    {
        _management = management;
    }

    [HttpGet("services")]
    public async Task<ActionResult<IEnumerable<ServiceCatalogDto>>> GetServices()
    {
        return Ok(await _management.GetServiceCatalogAsync());
    }

    [HttpGet("device-catalog")]
    public async Task<ActionResult<IEnumerable<DeviceCatalogDto>>> GetDeviceCatalog()
    {
        return Ok(await _management.GetDeviceCatalogAsync());
    }

    [HttpPost("device-catalog")]
    public async Task<ActionResult<DeviceCatalogDto>> CreateDeviceCatalog([FromBody] DeviceCatalogDto dto)
    {
        try
        {
            return Ok(await _management.CreateDeviceCatalogAsync(dto));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("device-catalog/{deviceCatalogId}")]
    public async Task<ActionResult<DeviceCatalogDto>> UpdateDeviceCatalog(int deviceCatalogId, [FromBody] DeviceCatalogDto dto)
    {
        try
        {
            return Ok(await _management.UpdateDeviceCatalogAsync(deviceCatalogId, dto));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("device-catalog/{deviceCatalogId}")]
    public async Task<IActionResult> DeleteDeviceCatalog(int deviceCatalogId)
    {
        try
        {
            await _management.DeleteDeviceCatalogAsync(deviceCatalogId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("tenants/candidates")]
    public async Task<ActionResult<IEnumerable<TenantPickerDto>>> GetTenantCandidates()
    {
        return Ok(await _management.GetTenantCandidatesAsync());
    }

    [HttpPost("rooms/{roomId}/images")]
    public async Task<ActionResult<RoomImageDto>> AddImage(int roomId, [FromBody] CreateRoomImageDto dto)
    {
        var image = await _management.AddRoomImageAsync(roomId, dto);
        return Ok(image);
    }

    [HttpPost("rooms/{roomId}/images/upload")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<RoomImageDto>> UploadImage(int roomId, IFormFile file)
    {
        try
        {
            var image = await _management.UploadRoomImageAsync(roomId, file);
            return Ok(image);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("rooms/{roomId}/images/{imageId}")]
    public async Task<IActionResult> DeleteImage(int roomId, int imageId)
    {
        await _management.DeleteRoomImageAsync(roomId, imageId);
        return NoContent();
    }

    [HttpPost("rooms/{roomId}/devices")]
    public async Task<ActionResult<RoomDeviceDto>> AddDevice(int roomId, [FromBody] CreateDeviceDto dto)
    {
        return Ok(await _management.AddDeviceAsync(roomId, dto));
    }

    [HttpPut("rooms/{roomId}/devices/{deviceId}")]
    public async Task<ActionResult<RoomDeviceDto>> UpdateDevice(int roomId, int deviceId, [FromBody] UpdateDeviceDto dto)
    {
        return Ok(await _management.UpdateDeviceAsync(roomId, deviceId, dto));
    }

    [HttpDelete("rooms/{roomId}/devices/{deviceId}")]
    public async Task<IActionResult> DeleteDevice(int roomId, int deviceId)
    {
        await _management.DeleteDeviceAsync(roomId, deviceId);
        return NoContent();
    }

    [HttpPost("rooms/{roomId}/devices/{deviceId}/upload-image")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<RoomDeviceDto>> UploadDeviceImage(int roomId, int deviceId, IFormFile file)
    {
        try
        {
            var device = await _management.UploadDeviceImageAsync(roomId, deviceId, file);
            return Ok(device);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("rooms/{roomId}/services")]
    public async Task<ActionResult<RoomServiceItemDto>> AssignService(int roomId, [FromBody] AssignRoomServiceDto dto)
    {
        return Ok(await _management.AssignServiceAsync(roomId, dto));
    }

    [HttpPut("rooms/{roomId}/services/{roomServiceId}")]
    public async Task<ActionResult<RoomServiceItemDto>> UpdateRoomService(int roomId, int roomServiceId, [FromBody] UpdateRoomServiceDto dto)
    {
        return Ok(await _management.UpdateRoomServiceAsync(roomId, roomServiceId, dto));
    }

    [HttpDelete("rooms/{roomId}/services/{roomServiceId}")]
    public async Task<IActionResult> DeleteRoomService(int roomId, int roomServiceId)
    {
        await _management.DeleteRoomServiceAsync(roomId, roomServiceId);
        return NoContent();
    }

    [HttpPost("rooms/{roomId}/tenants")]
    public async Task<ActionResult<TenantAssignmentDto>> AssignTenant(int roomId, [FromBody] AssignTenantDto dto)
    {
        return Ok(await _management.AssignTenantAsync(roomId, dto));
    }

    [HttpDelete("rooms/{roomId}/tenants/{contractId}")]
    public async Task<IActionResult> RemoveTenant(int roomId, int contractId)
    {
        await _management.RemoveTenantAsync(roomId, contractId);
        return NoContent();
    }

    [HttpPost("services")]
    public async Task<ActionResult<ServiceCatalogDto>> CreateService([FromBody] ServiceCatalogDto dto)
    {
        try
         {
             return Ok(await _management.CreateServiceAsync(dto));
         }
         catch (InvalidOperationException ex)
         {
             return BadRequest(new { message = ex.Message });
         }
    }

    [HttpPut("services/{serviceId}")]
    public async Task<ActionResult<ServiceCatalogDto>> UpdateService(int serviceId, [FromBody] ServiceCatalogDto dto)
    {
        try
        {
            return Ok(await _management.UpdateServiceAsync(serviceId, dto));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("services/{serviceId}")]
    public async Task<IActionResult> DeleteService(int serviceId)
    {
        try
        {
            await _management.DeleteServiceAsync(serviceId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
