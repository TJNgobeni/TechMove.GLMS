namespace TechMove.GLMS.Core.DTOs.ServiceRequests;

public class ServiceRequestCreateRequest
{
    public int ContractId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
}
