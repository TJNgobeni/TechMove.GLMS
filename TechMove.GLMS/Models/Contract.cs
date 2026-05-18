using System.ComponentModel.DataAnnotations;
using TechMove.GLMS.Models.Enums;

namespace TechMove.GLMS.Models
{
    public class Contract
    {
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }
        public Client Client { get; set; } = null!;

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [StringLength(100)]
        [Display(Name = "Service Level")]
        public string ServiceLevel { get; set; } = string.Empty;

        [Display(Name = "Signed Agreement")]
        public string SignedAgreementPath { get; set; } = string.Empty;

        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

        // Computed helpers (not mapped to DB)
        public int DaysRemaining => Math.Max(0, (EndDate - DateTime.Now).Days);
        public bool IsExpiredByDate => DateTime.Now > EndDate;
    }
}