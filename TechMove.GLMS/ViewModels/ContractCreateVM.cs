using Microsoft.AspNetCore.Mvc.Rendering;
using TechMove.GLMS.Models.Enums;

namespace TechMove.GLMS.ViewModels
{
    public class ContractCreateVM
    {
        public int ClientId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public ContractStatus Status { get; set; }

        public string ServiceLevel { get; set; }

        public IFormFile? SignedAgreement { get; set; }

        // dropdown data
        public IEnumerable<SelectListItem> Clients { get; set; }
    }
}