using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProofPocket.Data;
using ProofPocket.Models;
using ProofPocket.ViewModels;

namespace ProofPocket.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ---------------------------------------------------------------
        // DASHBOARD
        // ---------------------------------------------------------------

        public async Task<IActionResult> Index()
        {
            var users = (await _userManager.GetUsersInRoleAsync(AppRoles.User))
                .OrderByDescending(u => u.RegistrationDate)
                .ToList();

            var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            var products = await _db.Products.ToListAsync();

            var model = new AdminDashboardViewModel
            {
                RegisteredUsers = users.Count,
                ProductCategories = categories.Count,
                TotalProducts = products.Count,
                // Warranties that are still running (not expired).
                TotalWarranties = products.Count(p => p.Status != WarrantyStatus.Expired),
                Users = users,
                Categories = categories
            };

            return View(model);
        }

        // ---------------------------------------------------------------
        // USERS
        // ---------------------------------------------------------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                TempData["Error"] = "Administrator accounts cannot be deleted.";
                return Redirect(Url.Action(nameof(Index)) + "#users");
            }

            // The user's products are removed too (cascade delete).
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
                TempData["Success"] = $"User \"{user.FullName}\" was deleted.";
            else
                TempData["Error"] = "The user could not be deleted.";

            return Redirect(Url.Action(nameof(Index)) + "#users");
        }

        // ---------------------------------------------------------------
        // CATEGORIES
        // ---------------------------------------------------------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory([Bind("Name,Description")] Category category)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Category name is required (max 100 characters).";
                return Redirect(Url.Action(nameof(Index)) + "#categories");
            }

            category.Name = category.Name.Trim();

            if (await _db.Categories.AnyAsync(c => c.Name == category.Name))
            {
                TempData["Error"] = $"A category named \"{category.Name}\" already exists.";
                return Redirect(Url.Action(nameof(Index)) + "#categories");
            }

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Category \"{category.Name}\" was added.";
            return Redirect(Url.Action(nameof(Index)) + "#categories");
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, [Bind("Id,Name,Description")] Category model)
        {
            if (id != model.Id) return BadRequest();

            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            model.Name = model.Name?.Trim() ?? string.Empty;

            if (ModelState.IsValid &&
                await _db.Categories.AnyAsync(c => c.Name == model.Name && c.Id != id))
            {
                ModelState.AddModelError(nameof(model.Name), "Another category already uses this name.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            category.Name = model.Name;
            category.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Category \"{category.Name}\" was updated.";
            return Redirect(Url.Action(nameof(Index)) + "#categories");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            if (await _db.Products.AnyAsync(p => p.CategoryId == id))
            {
                TempData["Error"] = $"\"{category.Name}\" cannot be deleted because products are using it.";
                return Redirect(Url.Action(nameof(Index)) + "#categories");
            }

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Category \"{category.Name}\" was deleted.";
            return Redirect(Url.Action(nameof(Index)) + "#categories");
        }
    }
}
