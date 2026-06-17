using Microsoft.AspNetCore.Http;

namespace TechMove.GLMS.Core.DTOs.Contracts;

public class ContractCreateRequest
{
    public int ClientId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ServiceLevel { get; set; } = string.Empty;
    public IFormFile? SignedAgreement { get; set; }
}
