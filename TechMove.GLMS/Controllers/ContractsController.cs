using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechMove.GLMS.Data;
using TechMove.GLMS.Models;
using TechMove.GLMS.Models.Enums;
using TechMove.GLMS.Services;

namespace TechMove.GLMS.Controllers
{
    public class ContractsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IValidationService _validationService;
        private readonly ILogger<ContractsController> _logger;

        private static readonly string[] AllowedExtensions = { ".pdf" };
        private const long MaxFileSize = 5 * 1024 * 1024;

        public ContractsController(
            AppDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IValidationService validationService,
            ILogger<ContractsController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _validationService = validationService;
            _logger = logger;
        }

        // GET: Contracts
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? status)
        {
            var query = _context.Contracts
                .Include(c => c.Client)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(c => c.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.EndDate <= endDate.Value);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            var result = await query
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            return View(result);
        }

        // GET: Create
        public IActionResult Create()
        {
            PopulateViewData();
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contract contract, IFormFile? signedAgreement)
        {
            // Always reload dropdown data so the form re-renders correctly on error
            PopulateViewData();

            try
            {
                _logger.LogInformation("Creating contract for ClientId {ClientId}", contract.ClientId);

                // Validate client selection
                if (contract.ClientId <= 0)
                {
                    ModelState.AddModelError(nameof(contract.ClientId), "Please select a valid client.");
                }

                // Validate date range
                var dateValidation = _validationService.ValidateDateRange(contract.StartDate, contract.EndDate);
                if (!dateValidation.IsValid)
                {
                    ModelState.AddModelError(nameof(contract.EndDate), dateValidation.Message);
                }

                // Handle optional file upload
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    var fileValidation = _validationService.ValidateFileUpload(
                        signedAgreement,
                        AllowedExtensions,
                        MaxFileSize
                    );

                    if (!fileValidation.IsValid)
                    {
                        ModelState.AddModelError(nameof(contract.SignedAgreementPath), fileValidation.Message);
                    }
                    else
                    {
                        contract.SignedAgreementPath = await SaveFileAsync(signedAgreement);
                    }
                }

                // Log any validation errors for debugging
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        _logger.LogWarning("Validation error [{Key}]: {Error}", key, error.ErrorMessage);
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(contract);
                }

                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contract saved successfully with ID {Id}", contract.Id);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract");
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(contract);
            }
        }

        // GET: Details
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);

            return contract == null ? NotFound() : View(contract);
        }

        // GET: Download
        public IActionResult Download(int id)
        {
            var contract = _context.Contracts.Find(id);

            if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementPath))
                return NotFound();

            var path = Path.Combine(
                _webHostEnvironment.WebRootPath,
                contract.SignedAgreementPath.TrimStart('/')
            );

            if (!System.IO.File.Exists(path))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(path);
            return File(fileBytes, "application/pdf", Path.GetFileName(path));
        }

        // ───── HELPERS ─────

        private void PopulateViewData()
        {
            ViewData["Clients"] = _context.Clients.OrderBy(c => c.Name).ToList();
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("File saved: {File}", fileName);

            return $"/uploads/{fileName}";
        }
    }
}