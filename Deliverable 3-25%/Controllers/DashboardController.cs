
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProofPocket.Models;
using ProofPocket.Repositories;
using ProofPocket.ViewModels;

namespace ProofPocket.Controllers
{
    [Authorize(Roles = AppRoles.User)]
    public class DashboardController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IDashboardRepository _dashboardRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
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

        // GET: Dashboard/Index
        public async Task<IActionResult> Index()
        {
            var viewModel = await BuildDashboardAsync(
                new ProductFormViewModel());

            return View(viewModel);
        }

        // POST: Dashboard/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(
            [Bind(Prefix = "NewProduct")] ProductFormViewModel viewModel)
        {
            await ValidateProductAsync(viewModel);

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                    return ValidationError();

                return View(
                    "Index",
                    await BuildDashboardAsync(viewModel));
            }

            var userId = _userManager.GetUserId(User)!;

            var product = new Product
            {
                UserId = userId
            };

            Apply(product, viewModel);

            int newId = await _productRepository.AddAsync(product);

            if (IsAjaxRequest())
            {
                var category = await _categoryRepository
                    .GetByIdAsync(product.CategoryId);

                var expiryDate = product.PurchaseDate.Date
                    .AddMonths(product.WarrantyMonths);

                int daysLeft = (expiryDate.Date - DateTime.Today).Days;

                string statusText = daysLeft < 0
                    ? "Expired"
                    : daysLeft <= Product.ExpiringSoonDays
                        ? "Expiring Soon"
                        : "Active";

                string statusCss = daysLeft < 0
                    ? "status-expired"
                    : daysLeft <= Product.ExpiringSoonDays
                        ? "status-expiring"
                        : "status-active";

                string token = System.Net.WebUtility.HtmlEncode(
                    Request.Form["__RequestVerificationToken"].ToString());

                string rowHtml = $@"
<tr id='product-{newId}'>
    <td>{System.Net.WebUtility.HtmlEncode(product.Name)}</td>
    <td>{System.Net.WebUtility.HtmlEncode(category?.Name ?? "")}</td>
    <td>{System.Net.WebUtility.HtmlEncode(product.Brand)}</td>
    <td>{product.PurchaseDate:dd MMM yyyy}</td>
    <td>{expiryDate:dd MMM yyyy}</td>
    <td>
        <span class='status-badge {statusCss}'>{statusText}</span>
    </td>
    <td class='text-nowrap'>
        <a href='/Dashboard/Edit/{newId}' class='btn-action'>
            Edit
        </a>

        <form action='/Dashboard/DeleteProduct/{newId}'
              method='post'
              class='d-inline delete-product-form'>

            <input name='__RequestVerificationToken'
                   type='hidden'
                   value='{token}' />

            <button type='submit'
                    class='btn-action btn-action-danger'>
                Delete
            </button>
        </form>
    </td>
</tr>";

                return Json(new
                {
                    success = true,
                    message = "Product added successfully.",
                    id = newId,
                    rowHtml
                });
            }

            TempData["Success"] =
                $"\"{product.Name}\" was added to your products.";

            return Redirect(Url.Action(nameof(Index)) + "#products");
        }

        // GET: Dashboard/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);

            if (product == null ||
                product.UserId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            var viewModel = new ProductFormViewModel
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
            };

            await LoadCategoriesAsync();

            return View(viewModel);
        }

        // POST: Dashboard/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ProductFormViewModel viewModel)
        {
            if (id != viewModel.Id)
                return BadRequest();

            var product = await _productRepository.GetByIdAsync(id);

            if (product == null ||
                product.UserId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            await ValidateProductAsync(viewModel);

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                    return ValidationError();

                await LoadCategoriesAsync();

                return View(viewModel);
            }

            Apply(product, viewModel);

            bool updated = await _productRepository.UpdateAsync(product);

            if (!updated)
                return NotFound();

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = "Product updated successfully."
                });
            }

            TempData["Success"] = "Product updated successfully.";

            return Redirect(Url.Action(nameof(Index)) + "#products");
        }

        // POST: Dashboard/DeleteProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            bool deleted = await _productRepository.DeleteAsync(
                id, userId);

            if (!deleted)
            {
                if (IsAjaxRequest())
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Product not found."
                    });
                }

                TempData["Error"] = "Product could not be found.";

                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = "Product deleted successfully."
                });
            }

            TempData["Success"] = "Product deleted successfully.";

            return Redirect(Url.Action(nameof(Index)) + "#products");
        }

        // Builds the user dashboard.
        private async Task<UserDashboardViewModel> BuildDashboardAsync(
            ProductFormViewModel form)
        {
            var userId = _userManager.GetUserId(User)!;

            var user = await _userManager.GetUserAsync(User);

            var products = (await _productRepository
                .GetByUserIdAsync(userId))
                .OrderByDescending(p => p.Id)
                .ToList();

            var stats = await _dashboardRepository
                .GetUserStatsAsync(userId);

            var categories = await _categoryRepository.GetAllAsync();

            ViewBag.Categories = new SelectList(
                categories,
                "Id",
                "Name");

            return new UserDashboardViewModel
            {
                FullName = user?.FullName ?? "",

                TotalProducts = stats.TotalProducts,
                ActiveWarranties = stats.ActiveWarranties,
                ExpiringSoon = stats.ExpiringSoon,
                ExpiredWarranties = stats.ExpiredWarranties,

                Products = products,

                ExpiringSoonProducts = products
                    .Where(p => p.Status == WarrantyStatus.ExpiringSoon)
                    .OrderBy(p => p.DaysLeft)
                    .ToList(),

                NewProduct = form
            };
        }

        // Loads categories for product forms.
        private async Task LoadCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();

            ViewBag.Categories = new SelectList(
                categories,
                "Id",
                "Name");
        }

        // Server-side product validation.
        private async Task ValidateProductAsync(
            ProductFormViewModel form)
        {
            if (!form.PurchaseDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(form.PurchaseDate),
                    "Purchase date is required.");
            }
            else if (form.PurchaseDate.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(form.PurchaseDate),
                    "Purchase date cannot be in the future.");
            }

            if (!form.CategoryId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(form.CategoryId),
                    "Please select a category.");
            }
            else
            {
                var category = await _categoryRepository
                    .GetByIdAsync(form.CategoryId.Value);

                if (category == null)
                {
                    ModelState.AddModelError(
                        nameof(form.CategoryId),
                        "Please select a valid category.");
                }
            }

            if (!form.Price.HasValue || form.Price.Value < 0)
            {
                ModelState.AddModelError(
                    nameof(form.Price),
                    "Please enter a valid price.");
            }

            if (!form.WarrantyMonths.HasValue ||
                form.WarrantyMonths.Value < 1 ||
                form.WarrantyMonths.Value > 120)
            {
                ModelState.AddModelError(
                    nameof(form.WarrantyMonths),
                    "Warranty must be between 1 and 120 months.");
            }
        }

        // Copies form values into the Product model.
        private static void Apply(
            Product product,
            ProductFormViewModel form)
        {
            product.Name = form.Name.Trim();
            product.CategoryId = form.CategoryId.GetValueOrDefault();
            product.Brand = form.Brand.Trim();
            product.Model = form.Model?.Trim();
            product.Store = form.Store?.Trim();
            product.Price = form.Price.GetValueOrDefault();
            product.PurchaseDate = form.PurchaseDate.GetValueOrDefault();
            product.WarrantyMonths = form.WarrantyMonths.GetValueOrDefault();
            product.InvoiceNumber = form.InvoiceNumber?.Trim();
        }

        // Checks whether the request came from AJAX.
        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"]
                == "XMLHttpRequest";
        }

        // Returns validation errors for AJAX requests.
        private IActionResult ValidationError()
        {
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
                message = "Please correct the highlighted errors.",
                errors
            });
        }
    }
}
