using Bakery_Management_System.Data;
using Bakery_Management_System.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bakery_Management_System.Controllers
{
    public class AdminController : Controller
    {
        private readonly BakeryDbContext db;
        private readonly IWebHostEnvironment _env;

        public AdminController(BakeryDbContext db, IWebHostEnvironment env)
        {
            this.db = db;
            this._env = env;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "ADMIN";
        }

        private IActionResult? SecureAccess()
        {
            if (!IsAdmin())
            {
                TempData["ToastError"] = "Access denied. Admin authentication required.";
                return RedirectToAction("login", "Customer");
            }
            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "Admin";
            return null;
        }

        // ==========================================
        // 1. DASHBOARD
        // ==========================================
        [HttpGet]
        public IActionResult Dashboard()
        {
            var check = SecureAccess();
            if (check != null) return check;

            ViewBag.TotalProducts = db.Products.Count();
            ViewBag.TotalCustomers = db.Users.Count(u => u.UserRole == "CUSTOMER");
            ViewBag.TotalOrders = db.Orders.Count();
            ViewBag.TotalCategories = db.ProductCategories.Count();
            ViewBag.LowStockProducts = db.Products.Count(p => p.ProductQuantity <= 5);

            // Status counts
            ViewBag.PendingOrders = db.Orders.Count(o => o.OrderStatus == "Pending");
            ViewBag.ConfirmedOrders = db.Orders.Count(o => o.OrderStatus == "Confirmed");
            ViewBag.PreparingOrders = db.Orders.Count(o => o.OrderStatus == "Preparing");
            ViewBag.OutForDeliveryOrders = db.Orders.Count(o => o.OrderStatus == "Out for Delivery");
            ViewBag.DeliveredOrders = db.Orders.Count(o => o.OrderStatus == "Delivered" || o.OrderStatus == "Completed");
            ViewBag.CancelledOrders = db.Orders.Count(o => o.OrderStatus == "Cancelled" || o.OrderStatus == "Rejected");

            // Revenue calculation
            var validStatuses = new[] { "Confirmed", "Preparing", "Out for Delivery", "Delivered", "Completed" };
            ViewBag.TotalRevenue = db.Orders
                .Where(o => validStatuses.Contains(o.OrderStatus))
                .SelectMany(o => o.OrderItems)
                .Sum(oi => (decimal?)oi.Price) ?? 0;

            // Recent Orders (5)
            ViewBag.RecentOrders = db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            // Low Stock Products list (up to 5)
            ViewBag.LowStockList = db.Products
                .Include(p => p.Category)
                .Where(p => p.ProductQuantity <= 5)
                .OrderBy(p => p.ProductQuantity)
                .Take(5)
                .ToList();

            return View();
        }

        // ==========================================
        // 2. PRODUCT MANAGEMENT (CRUD)
        // ==========================================
        [HttpGet]
        public IActionResult ProductList(string search, int? categoryId, int page = 1)
        {
            var check = SecureAccess();
            if (check != null) return check;

            int pageSize = 10;
            var query = db.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(term) || (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(term)));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var products = query
                .OrderByDescending(p => p.ProductId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Categories = db.ProductCategories.ToList();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalItems;

            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            var check = SecureAccess();
            if (check != null) return check;

            ViewBag.Categories = db.ProductCategories.ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile? ProductImageFile)
        {
            var check = SecureAccess();
            if (check != null) return check;

            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError("ProductName", "Product name is required.");
            }
            if (product.ProductPrice <= 0)
            {
                ModelState.AddModelError("ProductPrice", "Price must be greater than 0.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = db.ProductCategories.ToList();
                return View(product);
            }

            if (ProductImageFile != null && ProductImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img/products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid() + "_" + Path.GetFileName(ProductImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProductImageFile.CopyToAsync(stream);
                }

                product.ProductImage = "/img/products/" + fileName;
            }
            else if (string.IsNullOrEmpty(product.ProductImage))
            {
                product.ProductImage = "/img/cake_card.jpg";
            }

            db.Products.Add(product);
            await db.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Product '{product.ProductName}' added successfully!";
            return RedirectToAction("ProductList");
        }

        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var product = db.Products.Find(id);
            if (product == null)
            {
                TempData["ToastError"] = "Product not found.";
                return RedirectToAction("ProductList");
            }

            ViewBag.Categories = db.ProductCategories.ToList();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Product model, IFormFile? NewImage)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var product = db.Products.Find(model.ProductId);
            if (product == null)
            {
                TempData["ToastError"] = "Product not found.";
                return RedirectToAction("ProductList");
            }

            product.ProductName = model.ProductName;
            product.ProductPrice = model.ProductPrice;
            product.ProductQuantity = model.ProductQuantity;
            product.ProductDescription = model.ProductDescription;
            product.CategoryId = model.CategoryId;

            if (NewImage != null && NewImage.Length > 0)
            {
                if (!string.IsNullOrEmpty(product.ProductImage) && product.ProductImage.StartsWith("/img/products/"))
                {
                    var oldImagePath = Path.Combine(_env.WebRootPath, product.ProductImage.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                var fileName = Guid.NewGuid() + "_" + Path.GetFileName(NewImage.FileName);
                var uploadsFolder = Path.Combine(_env.WebRootPath, "img/products");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await NewImage.CopyToAsync(stream);
                }

                product.ProductImage = "/img/products/" + fileName;
            }

            await db.SaveChangesAsync();
            TempData["ToastSuccess"] = $"Product '{product.ProductName}' updated successfully!";
            return RedirectToAction("ProductList");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var product = db.Products.FirstOrDefault(p => p.ProductId == id);
            if (product != null)
            {
                if (!string.IsNullOrEmpty(product.ProductImage) && product.ProductImage.StartsWith("/img/products/"))
                {
                    var imagePath = Path.Combine(_env.WebRootPath, product.ProductImage.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                string name = product.ProductName;
                db.Products.Remove(product);
                await db.SaveChangesAsync();
                TempData["ToastSuccess"] = $"Product '{name}' deleted successfully.";
            }
            else
            {
                TempData["ToastError"] = "Product not found.";
            }

            return RedirectToAction("ProductList");
        }

        // ==========================================
        // 3. CATEGORY MANAGEMENT (CRUD)
        // ==========================================
        [HttpGet]
        public IActionResult Categories()
        {
            var check = SecureAccess();
            if (check != null) return check;

            var categories = db.ProductCategories
                .Include(c => c.Products)
                .OrderBy(c => c.CategoryName)
                .ToList();

            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(string CategoryName)
        {
            var check = SecureAccess();
            if (check != null) return check;

            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                TempData["ToastError"] = "Category name cannot be empty.";
                return RedirectToAction("Categories");
            }

            string name = CategoryName.Trim();
            if (db.ProductCategories.Any(c => c.CategoryName.ToLower() == name.ToLower()))
            {
                TempData["ToastError"] = $"Category '{name}' already exists.";
                return RedirectToAction("Categories");
            }

            db.ProductCategories.Add(new ProductCategory { CategoryName = name });
            await db.SaveChangesAsync();
            TempData["ToastSuccess"] = $"Category '{name}' created successfully!";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int CategoryId, string CategoryName)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var cat = db.ProductCategories.Find(CategoryId);
            if (cat == null)
            {
                TempData["ToastError"] = "Category not found.";
                return RedirectToAction("Categories");
            }

            if (string.IsNullOrWhiteSpace(CategoryName))
            {
                TempData["ToastError"] = "Category name cannot be empty.";
                return RedirectToAction("Categories");
            }

            string name = CategoryName.Trim();
            if (db.ProductCategories.Any(c => c.CategoryId != CategoryId && c.CategoryName.ToLower() == name.ToLower()))
            {
                TempData["ToastError"] = $"Category '{name}' already exists.";
                return RedirectToAction("Categories");
            }

            cat.CategoryName = name;
            await db.SaveChangesAsync();
            TempData["ToastSuccess"] = $"Category renamed to '{name}'.";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var cat = db.ProductCategories.Include(c => c.Products).FirstOrDefault(c => c.CategoryId == id);
            if (cat == null)
            {
                TempData["ToastError"] = "Category not found.";
                return RedirectToAction("Categories");
            }

            if (cat.Products.Any())
            {
                TempData["ToastError"] = $"Cannot delete category '{cat.CategoryName}' because it contains {cat.Products.Count} product(s). Please reassign or delete the products first.";
                return RedirectToAction("Categories");
            }

            string name = cat.CategoryName;
            db.ProductCategories.Remove(cat);
            await db.SaveChangesAsync();
            TempData["ToastSuccess"] = $"Category '{name}' deleted successfully.";
            return RedirectToAction("Categories");
        }

        // ==========================================
        // 4. CUSTOMER MANAGEMENT
        // ==========================================
        [HttpGet]
        public IActionResult Customers(string search)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var query = db.Users.Where(u => u.UserRole == "CUSTOMER").AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(u => u.Username.ToLower().Contains(term));
            }

            var customers = query
                .Include(u => u.Orders)
                .ThenInclude(o => o.OrderItems)
                .OrderByDescending(u => u.UserId)
                .ToList();

            ViewBag.CurrentSearch = search;
            return View(customers);
        }

        // ==========================================
        // 5. ORDER MANAGEMENT (Pipelines)
        // ==========================================
        [HttpGet]
        public IActionResult AllOrders(string statusFilter, string search, int page = 1)
        {
            var check = SecureAccess();
            if (check != null) return check;

            int pageSize = 10;
            var query = db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(o => o.OrderStatus == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(o => o.OrderId.ToString().Contains(term) || (o.User != null && o.User.Username.ToLower().Contains(term)));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var orders = query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentStatus = statusFilter;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalItems;

            return View(orders);
        }

        [HttpGet]
        public IActionResult CustomerOrders()
        {
            var check = SecureAccess();
            if (check != null) return check;

            var orders = db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderStatus == "Pending" || o.OrderStatus == "Rejected" || o.OrderStatus == "Cancelled")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        [HttpGet]
        public IActionResult ConfirmedOrders()
        {
            var check = SecureAccess();
            if (check != null) return check;

            var activeStatuses = new[] { "Confirmed", "Preparing", "Out for Delivery" };
            var orders = db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => activeStatuses.Contains(o.OrderStatus))
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string newStatus)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var order = await db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                TempData["ToastError"] = "Order not found.";
                return RedirectToAction("AllOrders");
            }

            string oldStatus = order.OrderStatus ?? "Pending";
            string[] validStatuses = { "Pending", "Confirmed", "Preparing", "Out for Delivery", "Delivered", "Completed", "Cancelled", "Rejected" };

            if (!validStatuses.Contains(newStatus))
            {
                TempData["ToastError"] = "Invalid status requested.";
                return RedirectToAction("AllOrders");
            }

            // Restore product quantities if cancelling/rejecting an active order
            if ((newStatus == "Cancelled" || newStatus == "Rejected") && oldStatus != "Cancelled" && oldStatus != "Rejected")
            {
                foreach (var item in order.OrderItems)
                {
                    var product = await db.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.ProductQuantity += item.Quantity;
                    }
                }
            }

            order.OrderStatus = newStatus;
            await db.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Order #{id} status updated from '{oldStatus}' to '{newStatus}'.";
            return RedirectToAction("AllOrders");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            return await UpdateOrderStatus(id, "Confirmed");
        }

        [HttpPost]
        public async Task<IActionResult> RejectOrder(int id)
        {
            return await UpdateOrderStatus(id, "Rejected");
        }

        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            return await UpdateOrderStatus(id, "Delivered");
        }

        // ==========================================
        // 6. INVENTORY MANAGEMENT
        // ==========================================
        [HttpGet]
        public IActionResult Inventory(string search, bool? lowStockOnly)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var query = db.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(term));
            }

            if (lowStockOnly.HasValue && lowStockOnly.Value)
            {
                query = query.Where(p => p.ProductQuantity <= 5);
            }

            var products = query.OrderBy(p => p.ProductQuantity).ToList();

            ViewBag.CurrentSearch = search;
            ViewBag.LowStockOnly = lowStockOnly ?? false;
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int ProductId, int NewQuantity)
        {
            var check = SecureAccess();
            if (check != null) return check;

            var product = await db.Products.FindAsync(ProductId);
            if (product == null)
            {
                TempData["ToastError"] = "Product not found.";
                return RedirectToAction("Inventory");
            }

            if (NewQuantity < 0)
            {
                TempData["ToastError"] = "Stock quantity cannot be negative.";
                return RedirectToAction("Inventory");
            }

            int oldQty = product.ProductQuantity;
            product.ProductQuantity = NewQuantity;
            await db.SaveChangesAsync();

            TempData["ToastSuccess"] = $"Stock for '{product.ProductName}' updated from {oldQty} to {NewQuantity}.";
            return RedirectToAction("Inventory");
        }

        // ==========================================
        // 7. LOGOUT
        // ==========================================
        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["ToastSuccess"] = "You have been logged out.";
            return RedirectToAction("login", "Customer");
        }
    }
}
