using System.ComponentModel.DataAnnotations;
using ProofPocket.Models;

namespace ProofPocket.ViewModels
{
    /// <summary>Form model used to add and edit a product.</summary>
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Brand is required.")]
        [StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Model { get; set; }

        [StringLength(100)]
        public string? Store { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 1000000000, ErrorMessage = "Enter a valid price.")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Purchase date is required.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Purchase Date")]
        public DateTime? PurchaseDate { get; set; }

        [Required(ErrorMessage = "Warranty period is required.")]
        [Range(1, 120, ErrorMessage = "Warranty must be between 1 and 120 months.")]
        [Display(Name = "Warranty Period (Months)")]
        public int? WarrantyMonths { get; set; }

        [StringLength(100)]
        [Display(Name = "Invoice / Reference Number")]
        public string? InvoiceNumber { get; set; }
    }

    public class UserDashboardViewModel
    {
        public string FullName { get; set; } = string.Empty;

        public int TotalProducts { get; set; }
        public int ActiveWarranties { get; set; }
        public int ExpiringSoon { get; set; }
        public int ExpiredWarranties { get; set; }

        public List<Product> ExpiringSoonProducts { get; set; } = new();
        public List<Product> Products { get; set; } = new();

        public ProductFormViewModel NewProduct { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int RegisteredUsers { get; set; }
        public int ProductCategories { get; set; }
        public int TotalProducts { get; set; }
        public int TotalWarranties { get; set; }

        public List<ApplicationUser> Users { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
