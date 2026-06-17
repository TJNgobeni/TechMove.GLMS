namespace TechMove.GLMS.Core.Entities;

public class ServiceRequest
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public decimal CostZAR { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
