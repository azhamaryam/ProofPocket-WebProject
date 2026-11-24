using System.ComponentModel.DataAnnotations;

namespace ProofPocket.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
