using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TechMove.GLMS.Clients;
using TechMove.GLMS.Core.DTOs.Contracts;
using TechMove.GLMS.Models;
using TechMove.GLMS.Models.Enums;
using TechMove.GLMS.Services;

namespace TechMove.GLMS.Controllers;

public class ContractsController : Controller
{
    private readonly IApiClient _api;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IValidationService _validationService;
    private readonly ILogger<ContractsController> _logger;

    private static readonly string[] AllowedExtensions = { ".pdf" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    public ContractsController(
        IApiClient api,
        IWebHostEnvironment webHostEnvironment,
        IValidationService validationService,
        ILogger<ContractsController> logger)
    {
        _api = api;
        _webHostEnvironment = webHostEnvironment;
        _validationService = validationService;
        _logger = logger;
    }

    // GET: Contracts
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? status)
    {
        try
        {
            var filter = new ContractListFilter
            {
                StartFrom = startDate,
                EndTo = endDate,
                Status = status.HasValue ? (int?)status.Value : null
            };

            var result = await _api.GetContractsAsync(filter);
            return View(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading contracts");
            return View(new List<Models.Contract>());
        }
    }

    // GET: Create
    public async Task<IActionResult> Create()
    {
        await PopulateViewDataAsync();
        return View();
    }

    // POST: Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Contract contract, IFormFile? signedAgreement)
    {
        await PopulateViewDataAsync();

        try
        {
            _logger.LogInformation("Creating contract via API for ClientId {ClientId}", contract.ClientId);

            if (contract.ClientId <= 0)
            {
                ModelState.AddModelError(nameof(contract.ClientId), "Please select a valid client.");
            }

            var dateValidation = _validationService.ValidateDateRange(contract.StartDate, contract.EndDate);
            if (!dateValidation.IsValid)
            {
                ModelState.AddModelError(nameof(contract.EndDate), dateValidation.Message);
            }

            if (signedAgreement != null && signedAgreement.Length > 0)
            {
                var fileValidation = _validationService.ValidateFileUpload(signedAgreement, AllowedExtensions, MaxFileSize);
                if (!fileValidation.IsValid)
                {
                    ModelState.AddModelError(nameof(contract.SignedAgreementPath), fileValidation.Message);
                }
                else
                {
                    contract.SignedAgreementPath = await SaveFileAsync(signedAgreement);
                }
            }

            foreach (var key in ModelState.Keys)
            {
                foreach (var error in ModelState[key].Errors)
                {
                    _logger.LogWarning("Validation error {Key}: {Error}", key, error.ErrorMessage);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(contract);
            }

            var request = new ContractCreateRequest
            {
                ClientId = contract.ClientId,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                ServiceLevel = contract.ServiceLevel,
                SignedAgreement = signedAgreement
            };

            var created = await _api.CreateContractAsync(request);

            ModelState.Clear();
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
        try
        {
            var contract = await _api.GetContractAsync(id);
            if (contract == null)
                return NotFound();

            contract.ServiceRequests = (await _api.GetServiceRequestsAsync(id)).ToList();
            return View(contract);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading contract details for {Id}", id);
            return NotFound();
        }
    }

    // GET: Download
    public IActionResult Download(int id)
    {
        var pathArgument = Request.Query["path"].ToString();
        if (string.IsNullOrWhiteSpace(pathArgument))
            return NotFound();

        var path = Path.Combine(_webHostEnvironment.WebRootPath, pathArgument.TrimStart('/'));

        if (!System.IO.File.Exists(path))
            return NotFound();

        var fileBytes = System.IO.File.ReadAllBytes(path);
        return File(fileBytes, "application/pdf", Path.GetFileName(path));
    }

    // POST: Status
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, ContractStatus status)
    {
        try
        {
            await _api.PatchContractStatusAsync(id, status);
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contract status {Id}", id);
            ModelState.AddModelError("", "Unable to update status.");
            return RedirectToAction(nameof(Index));
        }
    }

    // ───── HELPERS ─────

    private async Task PopulateViewDataAsync()
    {
        var clients = await _api.GetClientsAsync();
        ViewData["Clients"] = clients;
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
