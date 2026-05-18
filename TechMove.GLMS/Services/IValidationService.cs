namespace TechMove.GLMS.Services
{
    public interface IValidationService
    {
        Task<(bool IsValid, string Message)> ValidateContractForServiceRequestAsync(int contractId);
        (bool IsValid, string Message) ValidateFileUpload(IFormFile file, string[] allowedExtensions, long maxFileSize);
        (bool IsValid, string Message) ValidateDateRange(DateTime startDate, DateTime endDate);
    }
}