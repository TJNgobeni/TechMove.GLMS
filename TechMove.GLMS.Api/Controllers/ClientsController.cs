using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.Data;
using Microsoft.EntityFrameworkCore;

namespace TechMove.GLMS.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(AppDbContext db, ILogger<ClientsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> Get(CancellationToken ct = default)
    {
        var data = await _db.Clients
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Region })
            .ToListAsync(ct);

        return Ok(data);
    }
}
