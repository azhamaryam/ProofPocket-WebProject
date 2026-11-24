using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProofPocket.Models
{
    public enum WarrantyStatus
    {
        Active,
        ExpiringSoon,
        Expired
    }

    public class Product
    {
        /// <summary>A warranty ending within this many days is "Expiring Soon".</summary>
        public const int ExpiringSoonDays = 30;

        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [Required, StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Model { get; set; }

        [StringLength(100)]
        public string? Store { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; }

        /// <summary>Warranty length in months.</summary>
        public int WarrantyMonths { get; set; }

        [StringLength(100)]
        public string? InvoiceNumber { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        // ----- Calculated (not stored in the database) -----

        [NotMapped]
        public DateTime ExpiryDate => PurchaseDate.Date.AddMonths(WarrantyMonths);

        [NotMapped]
        public int DaysLeft => (ExpiryDate.Date - DateTime.Today).Days;

        [NotMapped]
        public WarrantyStatus Status
        {
            get
            {
                if (DaysLeft < 0) return WarrantyStatus.Expired;
                if (DaysLeft <= ExpiringSoonDays) return WarrantyStatus.ExpiringSoon;
                return WarrantyStatus.Active;
            }
        }

        [NotMapped]
        public string StatusText => Status switch
        {
            WarrantyStatus.Active => "Active",
            WarrantyStatus.ExpiringSoon => "Expiring Soon",
            _ => "Expired"
        };

        [NotMapped]
        public string StatusCss => Status switch
        {
            WarrantyStatus.Active => "status-active",
            WarrantyStatus.ExpiringSoon => "status-expiring",
            _ => "status-expired"
        };
    }
}
