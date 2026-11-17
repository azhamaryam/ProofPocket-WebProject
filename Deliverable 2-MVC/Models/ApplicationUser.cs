using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ProofPocket.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}
