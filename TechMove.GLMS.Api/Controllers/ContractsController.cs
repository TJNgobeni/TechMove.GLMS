using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.Models.Enums;
using TechMove.GLMS.Services;
using TechMove.GLMS.Core.DTOs.Contracts;

namespace TechMove.GLMS.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly Services.ContractService _contractService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ContractsController> _logger;

    private static readonly string[] AllowedExtensions = { ".pdf" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    public ContractsController(
        Services.ContractService contractService,
        IWebHostEnvironment env,
        ILogger<ContractsController> logger)
    {
        _contractService = contractService;
        _env = env;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> Get([FromQuery] ContractListFilter filter, CancellationToken ct = default)
    {
        var data = await _contractService.GetAllAsync(filter, ct);
        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetById(int id, CancellationToken ct = default)
    {
        var data = await _contractService.GetByIdAsync(id, ct);
        if (data == null)
            return NotFound();
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] ContractCreateRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = await _contractService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = ((dynamic)result).Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> PatchStatus(int id, [FromBody] PatchContractStatusRequest request, CancellationToken ct = default)
    {
        try
        {
            await _contractService.UpdateStatusAsync(id, request.Status, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class PatchContractStatusRequest
{
    public TechMove.GLMS.Models.Enums.ContractStatus Status { get; set; }
}
