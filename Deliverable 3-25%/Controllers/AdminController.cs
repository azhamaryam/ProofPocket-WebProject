using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProofPocket.Models;
using ProofPocket.Repositories;
using ProofPocket.ViewModels;

namespace ProofPocket.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IDashboardRepository _dashboardRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IDashboardRepository dashboardRepository,
            UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _dashboardRepository = dashboardRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.GetUsersInRoleAsync(AppRoles.User);

            var categories = (await _categoryRepository.GetAllAsync()).ToList();
            var stats = await _dashboardRepository.GetAdminStatsAsync();

            var model = new AdminDashboardViewModel
            {
                RegisteredUsers = stats.TotalUsers,
                ProductCategories = stats.TotalCategories,
                TotalProducts = stats.TotalProducts,
                TotalWarranties = stats.ActiveWarranties,

                Users = users
                    .OrderByDescending(u => u.RegistrationDate)
                    .ToList(),

                Categories = categories
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return AjaxOrRedirectError(
                    "User not found.", "users");
            }

            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                return AjaxOrRedirectError(
                    "Admin accounts cannot be deleted here.", "users");
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return AjaxOrRedirectError(
                    "Unable to delete this user.", "users");
            }

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = "User deleted successfully."
                });
            }

            TempData["Success"] = "User deleted successfully.";
            return Redirect(Url.Action(nameof(Index)) + "#users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(Category model)
        {
            model.Name = model.Name?.Trim() ?? "";
            model.Description = model.Description?.Trim();

            if (!ModelState.IsValid)
                return CategoryValidationError();

            if (await _categoryRepository.ExistsByNameAsync(model.Name))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "This category already exists.");

                return CategoryValidationError();
            }

            int id = await _categoryRepository.AddAsync(model);

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = "Category added successfully.",
                    id
                });
            }

            TempData["Success"] = "Category added successfully.";
            return Redirect(Url.Action(nameof(Index)) + "#categories");
        }

        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(
            int id, Category model)
        {
            if (id != model.Id)
                return BadRequest();

            model.Name = model.Name?.Trim() ?? "";
            model.Description = model.Description?.Trim();

            if (!ModelState.IsValid)
                return CategoryValidationError();

            var existing = await _categoryRepository.GetByIdAsync(id);

            if (existing == null)
                return NotFound();

            if (await _categoryRepository.ExistsByNameAsync(model.Name, id))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Another category already uses this name.");

                return CategoryValidationError();
            }

            existing.Name = model.Name;
            existing.Description = model.Description;

            bool updated = await _categoryRepository.UpdateAsync(existing);

            if (!updated)
                return NotFound();

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = "Category updated successfully."
                });
            }

            TempData["Success"] = "Category updated successfully.";
            return Redirect(Url.Action(nameof(Index)) + "#categories");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                return AjaxOrRedirectError(
                    "Category not found.", "categories");
            }

            if (await _categoryRepository.HasProductsAsync(id))
            {
                return AjaxOrRedirectError(
                    "This category cannot be deleted because products are using it.",
                    "categories");
            }

            bool deleted = await _categoryRepository.DeleteAsync(id);

            if (!deleted)
            {
                return AjaxOrRedirectError(
                    "Category could not be deleted.", "categories");
            }

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = "Category deleted successfully."
                });
            }

            TempData["Success"] = "Category deleted successfully.";
            return Redirect(Url.Action(nameof(Index)) + "#categories");
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }

        private IActionResult CategoryValidationError()
        {
            if (!IsAjaxRequest())
            {
                TempData["Error"] = string.Join(
                    " ",
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                return RedirectToAction(nameof(Index));
            }

            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors
                        .Select(e => e.ErrorMessage)
                        .ToArray());

            return BadRequest(new
            {
                success = false,
                message = "Please correct the category details.",
                errors
            });
        }

        private IActionResult AjaxOrRedirectError(
            string message, string fragment)
        {
            if (IsAjaxRequest())
            {
                return BadRequest(new
                {
                    success = false,
                    message
                });
            }

            TempData["Error"] = message;
            return Redirect(Url.Action(nameof(Index)) + "#" + fragment);
        }
    }
}