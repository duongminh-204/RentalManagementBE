using Backend.DTOs.Contracts;
using Backend.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/contracts")]
[ApiController]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contracts;

    public ContractsController(IContractService contracts)
    {
        _contracts = contracts;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContractDto>>> GetAll(
        [FromQuery] int? roomId,
        [FromQuery] int? tenantId)
    {
        return Ok(await _contracts.GetAllAsync(roomId, tenantId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContractDto>> GetById(int id)
    {
        var contract = await _contracts.GetByIdAsync(id);
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

    [HttpPut("{id}")]
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

    [HttpDelete("{id}")]
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

    [HttpPost("{id}/renew")]
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
    }

    [HttpPost("{id}/upload-file")]
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

    [HttpGet("{id}/download-file")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var contract = await _contracts.GetByIdAsync(id);
        if (contract?.FileUrl == null)
            return NotFound();

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var relative = contract.FileUrl.TrimStart('/');
        var fullPath = Path.Combine(webRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(fullPath))
            return NotFound();

        var contentType = fullPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? "application/pdf"
            : "application/octet-stream";
        return PhysicalFile(fullPath, contentType, Path.GetFileName(fullPath));
    }
}
