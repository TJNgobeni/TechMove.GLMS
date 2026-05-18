using TechMove.GLMS.Data;
using TechMove.GLMS.Models.Enums;

namespace TechMove.GLMS.Services
{
    public class ValidationService : IValidationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(AppDbContext context, ILogger<ValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool IsValid, string Message)> ValidateContractForServiceRequestAsync(int contractId)
        {
            try
            {
                var contract = await _context.Contracts.FindAsync(contractId);

                if (contract == null)
                    return (false, "Contract not found.");

                if (contract.Status == ContractStatus.Expired)
                    return (false, "Cannot create a service request. The parent contract is Expired or On Hold.");

                if (contract.Status == ContractStatus.OnHold)
                    return (false, "Cannot create a service request. The parent contract is Expired or On Hold.");

                if (DateTime.Now > contract.EndDate)
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

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return (false, $"Only {string.Join(", ", allowedExtensions).ToUpper()} files are allowed.");

            return (true, "Valid");
        }

        public (bool IsValid, string Message) ValidateDateRange(DateTime startDate, DateTime endDate)
        {
            if (endDate <= startDate)
                return (false, "End date must be after start date.");

            return (true, "Valid");
        }
    }
}