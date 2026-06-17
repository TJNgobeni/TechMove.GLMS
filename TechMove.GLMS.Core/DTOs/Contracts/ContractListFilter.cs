namespace TechMove.GLMS.Core.DTOs.Contracts;

public class ContractListFilter
{
    public int? ClientId { get; set; }
    public DateTime? StartFrom { get; set; }
    public DateTime? EndTo { get; set; }
    public int? Status { get; set; }
}
