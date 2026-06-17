using Microsoft.AspNetCore.Mvc;
using TechMove.GLMS.Clients;
using TechMove.GLMS.Core.DTOs.ServiceRequests;
using TechMove.GLMS.Models.Enums;
using TechMove.GLMS.Services;

namespace TechMove.GLMS.Controllers;

public class ServiceRequestsController : Controller
{
    private readonly IApiClient _api;
    private readonly ICurrencyService _currencyService;
    private readonly IValidationService _validationService;
    private readonly ILogger<ServiceRequestsController> _logger;
    private const decimal DefaultRate = 18.50m;

    public ServiceRequestsController(
        IApiClient api,
        ICurrencyService currencyService,
        IValidationService validationService,
        ILogger<ServiceRequestsController> logger)
    {
        _api = api;
        _currencyService = currencyService;
        _validationService = validationService;
        _logger = logger;
    }

    // GET: ServiceRequests
    public async Task<IActionResult> Index()
    {
        try
        {
            var requests = await _api.GetServiceRequestsAsync();
            return View(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service requests");
            return View(new List<Models.ServiceRequest>());
        }
    }

    // GET: ServiceRequests/Create
    public async Task<IActionResult> Create()
    {
        await PopulateViewDataAsync();
        return View();
    }

    // POST: ServiceRequests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Models.ServiceRequest serviceRequest)
    {
        try
        {
            var validation = await _validationService.ValidateContractForServiceRequestAsync(serviceRequest.ContractId);
            if (!validation.IsValid)
            {
                ModelState.AddModelError("", validation.Message);
                await PopulateViewDataAsync();
                return View(serviceRequest);
            }

            try
            {
                var rate = await _currencyService.GetUsdToZarRateAsync();
                serviceRequest.CostZAR = serviceRequest.Cost * rate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Currency conversion failed. Using default rate.");
                serviceRequest.CostZAR = serviceRequest.Cost * DefaultRate;
            }

            if (!ModelState.IsValid)
            {
                await PopulateViewDataAsync();
                return View(serviceRequest);
            }

            var request = new ServiceRequestCreateRequest
            {
                ContractId = serviceRequest.ContractId,
                Description = serviceRequest.Description,
                Cost = serviceRequest.Cost
            };

            await _api.CreateServiceRequestAsync(request);
            _logger.LogInformation("Service request created for ContractId {ContractId}", serviceRequest.ContractId);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service request");
            ModelState.AddModelError("", "An unexpected error occurred.");
            await PopulateViewDataAsync();
            return View(serviceRequest);
        }
    }

    // GET: ServiceRequests/GetExchangeRate
    [HttpGet]
    public async Task<IActionResult> GetExchangeRate()
    {
        try
        {
            var rate = await _currencyService.GetUsdToZarRateAsync();
            return Json(new { rate, success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching exchange rate for client");
            return Json(new { rate = DefaultRate, success = false });
        }
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    private async Task PopulateViewDataAsync()
    {
        var contracts = await _api.GetContractsAsync();
        var activeContracts = contracts
            .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.Today)
            .OrderBy(c => c.Client?.Name)
            .ToList();

        ViewData["Contracts"] = activeContracts;
    }
}
