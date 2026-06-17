namespace TechMove.GLMS.Core.Models;

using TechMove.GLMS.Core.Models.Enums;

public class Contract
{
    public int Id { get; set; }

    public required int ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public string ServiceLevel { get; set; } = string.Empty;

    public string SignedAgreementPath { get; set; } = string.Empty;

    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
