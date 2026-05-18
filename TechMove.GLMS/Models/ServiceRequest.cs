using System.ComponentModel.DataAnnotations;

namespace TechMove.GLMS.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }

        [Required]
        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Cost must be greater than 0")]
        [Display(Name = "Cost (USD)")]
        [DataType(DataType.Currency)]
        public decimal Cost { get; set; }

        [Display(Name = "Cost (ZAR)")]
        [DataType(DataType.Currency)]
        public decimal CostZAR { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}