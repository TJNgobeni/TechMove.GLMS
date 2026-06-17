using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TechMove.GLMS.Api.Services;

public interface IValidationService
{
    Task<(bool IsValid, string Message)> ValidateContractForServiceRequestAsync(int contractId);
    (bool IsValid, string Message) ValidateFileUpload(IFormFile file, string[] allowedExtensions, long maxFileSize);
    (bool IsValid, string Message) ValidateDateRange(DateTime startDate, DateTime endDate);
}

public class ValidationService : IValidationService
{
    private readonly TechMove.GLMS.Data.AppDbContext _db;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(TechMove.GLMS.Data.AppDbContext db, ILogger<ValidationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(bool IsValid, string Message)> ValidateContractForServiceRequestAsync(int contractId)
    {
        try
        {
            var contract = await _db.Contracts.FindAsync(contractId);
            if (contract == null)
                return (false, "Contract not found.");

            var now = DateTime.Now;
            if (contract.Status == Core.Entities.ContractStatus.Expired || contract.Status == Core.Entities.ContractStatus.OnHold)
                return (false, "Cannot create a service request. The parent contract is Expired or On Hold.");

            if (now > contract.EndDate)
                return (false, "Cannot create a service request. The contract has passed its end date.");

            return (true, "Valid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating contract {ContractId}", contractId);
            return (false, "A validation error occurred.");
        }
    }

    public (bool IsValid, string Message) ValidateFileUpload(IFormFile file, string[] allowedExtensions, long maxFileSize)
    {
        if (file == null || file.Length == 0)
            return (false, "No file was uploaded.");

        if (file.Length > maxFileSize)
            return (false, $"File size exceeds the {maxFileSize / 1024 / 1024}MB limit.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return (false, $"Only {string.Join(", ", allowedExtensions).ToUpperInvariant()} files are allowed.");

        return (true, "Valid");
    }

    public (bool IsValid, string Message) ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            return (false, "End date must be after start date.");

        return (true, "Valid");
    }
}
