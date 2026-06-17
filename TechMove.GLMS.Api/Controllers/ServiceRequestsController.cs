using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.Core.DTOs.ServiceRequests;
using TechMove.GLMS.Core.Entities;
using TechMove.GLMS.Services;
using TechMove.GLMS.Data;
using Microsoft.EntityFrameworkCore;

namespace TechMove.GLMS.Api.Controllers;

[ApiController]
[Route("api/servicerequests")]
public class ServiceRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrencyService _currencyService;
    private readonly IValidationService _validationService;
    private readonly ILogger<ServiceRequestsController> _logger;
    private const decimal DefaultRate = 18.50m;

    public ServiceRequestsController(
        AppDbContext db,
        ICurrencyService currencyService,
        IValidationService validationService,
        ILogger<ServiceRequestsController> logger)
    {
        _db = db;
        _currencyService = currencyService;
        _validationService = validationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> Get([FromQuery] int? contractId, CancellationToken ct = default)
    {
        if (contractId.HasValue)
        {
            var items = await _db.ServiceRequests
                .Where(s => s.ContractId == contractId.Value)
                .Select(s => new
                {
                    s.Id,
                    s.ContractId,
                    s.Description,
                    s.Cost,
                    s.CostZAR,
                    s.Status,
                    s.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(items);
        }

        var all = await _db.ServiceRequests
            .Include(s => s.Contract)
            .ThenInclude(c => c.Client)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.ContractId,
                Client = new { s.Contract.Client.Id, s.Contract.Client.Name },
                s.Contract.ServiceLevel,
                s.Description,
                s.Cost,
                s.CostZAR,
                s.Status,
                s.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(all);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetById(int id, CancellationToken ct = default)
    {
        var item = await _db.ServiceRequests
            .Include(s => s.Contract)
            .ThenInclude(c => c.Client)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (item == null)
            return NotFound();

        return Ok(new
        {
            item.Id,
            item.ContractId,
            Client = new { item.Contract.Client.Id, item.Contract.Client.Name },
            item.Contract.ServiceLevel,
            item.Description,
            item.Cost,
            item.CostZAR,
            item.Status,
            item.CreatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ServiceRequestCreateRequest request, CancellationToken ct = default)
    {
        try
        {
            var validation = await _validationService.ValidateContractForServiceRequestAsync(request.ContractId);
            if (!validation.IsValid)
                return BadRequest(new { error = validation.Message });

            decimal costZar;
            try
            {
                var rate = await _currencyService.GetUsdToZarRateAsync();
                costZar = request.Cost * rate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Currency conversion failed. Using default rate.");
                costZar = request.Cost * DefaultRate;
            }

            var entity = new ServiceRequest
            {
                ContractId = request.ContractId,
                Description = request.Description,
                Cost = request.Cost,
                CostZAR = costZar,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.ServiceRequests.Add(entity);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new
            {
                entity.Id,
                entity.ContractId,
                entity.Description,
                entity.Cost,
                entity.CostZAR,
                entity.Status,
                entity.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service request");
            return BadRequest(new { error = "An unexpected error occurred." });
        }
    }
}
