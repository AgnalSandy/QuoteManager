using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteManager.Models
{
    public class ServiceTax
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public ServiceMaster Service { get; set; } = null!;

        [Required]
        public int TaxId { get; set; }

        [ForeignKey("TaxId")]
        public TaxMaster Tax { get; set; } = null!;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
