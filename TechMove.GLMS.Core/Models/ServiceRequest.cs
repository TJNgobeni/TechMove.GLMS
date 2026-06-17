namespace TechMove.GLMS.Core.Models;

public class ServiceRequest
{
    public int Id { get; set; }

    public required int ContractId { get; set; }

    public Contract Contract { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    public decimal Cost { get; set; }

    public decimal CostZAR { get; set; }

    public string Status { get; set; } = "Pending";
}
