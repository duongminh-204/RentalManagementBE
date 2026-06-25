using Backend.Authorization;
using Backend.DTOs.Contracts;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[Route("api/contracts")]
[ApiController]
[Authorize(Policy = AuthorizationPolicies.ActiveOwner)]
[Authorize(Policy = PackageFeaturePolicies.Contracts)]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contracts;
    private readonly IFileStorageService _fileStorage;

    public ContractsController(IContractService contracts, IFileStorageService fileStorage)
    {
        _contracts = contracts;
        _fileStorage = fileStorage;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractDto>>> GetAll(
        [FromQuery] int? roomId,
        [FromQuery] int? tenantId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = true)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        return Ok(await _contracts.GetAllAsync(roomId, tenantId, search, status, sortBy, sortDesc, userId));
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<IEnumerable<ContractDto>>> GetExpiring([FromQuery] int days = 30)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        return Ok(await _contracts.GetExpiringAsync(days, userId));
    }

    [HttpGet("reminders")]
    public async Task<ActionResult<IEnumerable<ContractReminderDto>>> GetReminders()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        return Ok(await _contracts.GetRemindersAsync(userId));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContractDto>> GetById(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var contract = await _contracts.GetByIdAsync(id, userId);
        return contract != null ? Ok(contract) : NotFound();
    }

    [HttpGet("{id:int}/detail")]
    public async Task<ActionResult<ContractDetailDto>> GetDetail(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var contract = await _contracts.GetDetailAsync(id, userId);
        return contract != null ? Ok(contract) : NotFound();
    }

    [HttpPost]
    public async Task<ActionResult<ContractDto>> Create([FromBody] CreateContractDto dto)
    {
        try
        {
            var created = await _contracts.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContractDto>> Update(int id, [FromBody] UpdateContractDto dto)
    {
        try
        {
            return Ok(await _contracts.UpdateAsync(id, dto));
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _contracts.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/renew")]
    public async Task<ActionResult<ContractDto>> Renew(int id, [FromBody] RenewContractDto dto)
    {
        try
        {
            return Ok(await _contracts.RenewAsync(id, dto));
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

    [HttpPost("{id:int}/terminate")]
    public async Task<ActionResult<ContractDto>> Terminate(int id, [FromBody] TerminateContractDto dto)
    {
        try
        {
            return Ok(await _contracts.TerminateAsync(id, dto));
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

    [HttpPut("{id:int}/deposit")]
    public async Task<ActionResult<ContractDto>> UpdateDeposit(int id, [FromBody] UpdateDepositDto dto)
    {
        try
        {
            return Ok(await _contracts.UpdateDepositAsync(id, dto));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id:int}/upload-file")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<object>> UploadFile(int id, IFormFile file)
    {
        try
        {
            var path = await _contracts.UploadFileAsync(id, file);
            return Ok(new { fileUrl = path });
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

    [HttpPost("{id:int}/generate")]
    public async Task<ActionResult<object>> Generate(int id, [FromBody] GenerateContractDto? dto)
    {
        try
        {
            var path = await _contracts.GenerateFromTemplateAsync(id, dto);
            return Ok(new { fileUrl = path });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:int}/download-file")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var contract = await _contracts.GetByIdAsync(id, userId);
        if (contract?.FileUrl == null)
            return NotFound();

        if (_fileStorage.IsRemoteUrl(contract.FileUrl))
            return Redirect(contract.FileUrl);

        var download = await _fileStorage.OpenReadAsync(contract.FileUrl);
        if (download == null)
            return NotFound();

        return File(download.Stream, download.ContentType, download.FileName);
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }
}
