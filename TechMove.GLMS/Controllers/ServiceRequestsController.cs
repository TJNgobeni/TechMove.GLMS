using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMove.GLMS.Data;
using TechMove.GLMS.Models;
using TechMove.GLMS.Models.Enums;
using TechMove.GLMS.Services;

namespace TechMove.GLMS.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrencyService _currencyService;
        private readonly IValidationService _validationService;
        private readonly ILogger<ServiceRequestsController> _logger;
        private const decimal DefaultRate = 18.50m;

        public ServiceRequestsController(
            AppDbContext context,
            ICurrencyService currencyService,
            IValidationService validationService,
            ILogger<ServiceRequestsController> logger)
        {
            _context = context;
            _currencyService = currencyService;
            _validationService = validationService;
            _logger = logger;
        }

        // GET: ServiceRequests
        public async Task<IActionResult> Index()
        {
            try
            {
                var requests = await _context.ServiceRequests
                    .Include(s => s.Contract)
                    .ThenInclude(c => c.Client)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return View(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service requests");
                return View(new List<ServiceRequest>());
            }
        }

        // GET: ServiceRequests/Create
        public IActionResult Create()
        {
            PopulateViewData();
            return View();
        }

        // POST: ServiceRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest serviceRequest)
        {
            try
            {
                var validation = await _validationService.ValidateContractForServiceRequestAsync(serviceRequest.ContractId);
                if (!validation.IsValid)
                {
                    ModelState.AddModelError("", validation.Message);
                    PopulateViewData();
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
                    PopulateViewData();
                    return View(serviceRequest);
                }

                _context.Add(serviceRequest);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Service request created for ContractId {ContractId}", serviceRequest.ContractId);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service request");
                ModelState.AddModelError("", "An unexpected error occurred.");
                PopulateViewData();
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

        private void PopulateViewData()
        {
            ViewData["Contracts"] = _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active && c.EndDate >= DateTime.Today)
                .OrderBy(c => c.Client.Name)
                .ToList();
        }
    }
}