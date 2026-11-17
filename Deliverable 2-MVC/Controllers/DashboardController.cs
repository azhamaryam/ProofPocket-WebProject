using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProofPocket.Data;
using ProofPocket.Models;
using ProofPocket.ViewModels;

namespace ProofPocket.Controllers
{
    [Authorize(Roles = AppRoles.User)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ---------------------------------------------------------------
        // DASHBOARD (stats + expiring soon + products + add form)
        // ---------------------------------------------------------------

        public async Task<IActionResult> Index()
        {
            return View(await BuildDashboardAsync(new ProductFormViewModel()));
        }

        // ---------------------------------------------------------------
        // ADD PRODUCT
        // ---------------------------------------------------------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct([Bind(Prefix = "NewProduct")] ProductFormViewModel model)
        {
            await ValidateProductAsync(model, "NewProduct.");

            if (!ModelState.IsValid)
            {
                return View("Index", await BuildDashboardAsync(model));
            }

            var product = new Product { UserId = _userManager.GetUserId(User)! };
            Apply(product, model);

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"\"{product.Name}\" was added to your products.";
            return Redirect(Url.Action(nameof(Index)) + "#products");
        }

        // ---------------------------------------------------------------
        // EDIT PRODUCT
        // ---------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await FindOwnProductAsync(id);
            if (product == null) return NotFound();

            await LoadCategoriesAsync();

            return View(new ProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                Brand = product.Brand,
                Model = product.Model,
                Store = product.Store,
                Price = product.Price,
                PurchaseDate = product.PurchaseDate,
                WarrantyMonths = product.WarrantyMonths,
                InvoiceNumber = product.InvoiceNumber
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
        {
            if (id != model.Id) return BadRequest();

            var product = await FindOwnProductAsync(id);
            if (product == null) return NotFound();

            await ValidateProductAsync(model, string.Empty);

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            Apply(product, model);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"\"{product.Name}\" was updated.";
            return Redirect(Url.Action(nameof(Index)) + "#products");
        }

        // ---------------------------------------------------------------
        // DELETE PRODUCT
        // ---------------------------------------------------------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await FindOwnProductAsync(id);
            if (product == null) return NotFound();

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"\"{product.Name}\" was deleted.";
            return Redirect(Url.Action(nameof(Index)) + "#products");
        }

        // ---------------------------------------------------------------
        // HELPERS
        // ---------------------------------------------------------------

        private async Task<UserDashboardViewModel> BuildDashboardAsync(ProductFormViewModel form)
        {
            var userId = _userManager.GetUserId(User)!;
            var user = await _userManager.GetUserAsync(User);

            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            await LoadCategoriesAsync();

            return new UserDashboardViewModel
            {
                FullName = user?.FullName ?? string.Empty,
                TotalProducts = products.Count,
                // "Active" means the warranty has not expired yet (this includes ones expiring soon).
                ActiveWarranties = products.Count(p => p.Status != WarrantyStatus.Expired),
                ExpiringSoon = products.Count(p => p.Status == WarrantyStatus.ExpiringSoon),
                ExpiredWarranties = products.Count(p => p.Status == WarrantyStatus.Expired),
                ExpiringSoonProducts = products
                    .Where(p => p.Status == WarrantyStatus.ExpiringSoon)
                    .OrderBy(p => p.ExpiryDate)
                    .ToList(),
                Products = products,
                NewProduct = form
            };
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = new SelectList(categories, nameof(Category.Id), nameof(Category.Name));
        }

        private async Task<Product?> FindOwnProductAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            return await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        }

        private async Task ValidateProductAsync(ProductFormViewModel model, string keyPrefix)
        {
            if (model.PurchaseDate.HasValue && model.PurchaseDate.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(keyPrefix + nameof(model.PurchaseDate),
                    "Purchase date cannot be in the future.");
            }

            if (model.CategoryId.HasValue &&
                !await _db.Categories.AnyAsync(c => c.Id == model.CategoryId.Value))
            {
                ModelState.AddModelError(keyPrefix + nameof(model.CategoryId),
                    "Please choose a valid category.");
            }
        }

        private static void Apply(Product product, ProductFormViewModel model)
        {
            product.Name = model.Name.Trim();
            product.CategoryId = model.CategoryId!.Value;
            product.Brand = model.Brand.Trim();
            product.Model = string.IsNullOrWhiteSpace(model.Model) ? null : model.Model.Trim();
            product.Store = string.IsNullOrWhiteSpace(model.Store) ? null : model.Store.Trim();
            product.Price = model.Price!.Value;
            product.PurchaseDate = model.PurchaseDate!.Value.Date;
            product.WarrantyMonths = model.WarrantyMonths!.Value;
            product.InvoiceNumber = string.IsNullOrWhiteSpace(model.InvoiceNumber) ? null : model.InvoiceNumber.Trim();
        }
    }
}
