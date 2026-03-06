using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuoteManager.Models
{
    public class QuoteItemTax
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuoteItemId { get; set; }

        [ForeignKey("QuoteItemId")]
        public QuoteItem QuoteItem { get; set; } = null!;

        [Required]
        public int TaxId { get; set; }

        [ForeignKey("TaxId")]
        public TaxMaster Tax { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tax Amount (₹)")]
        public decimal TaxAmount { get; set; }
    }
}
