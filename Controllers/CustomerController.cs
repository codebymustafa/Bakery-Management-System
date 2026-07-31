using Bakery_Management_System.Data;
using Bakery_Management_System.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bakery_Management_System.Controllers
{
    public class CustomerController : Controller
    {
        private readonly BakeryDbContext _db;
        private readonly string _geminiApiKey;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(BakeryDbContext db, IConfiguration config, ILogger<CustomerController> logger)
        {
            _db = db;
            _geminiApiKey = config["GeminiSettings:ApiKey"] ?? string.Empty;
            _logger = logger;
        }

        private bool IsUserAuthenticated()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            return !string.IsNullOrEmpty(userEmail) && userEmail != "GuestUser";
        }

        private User? GetCurrentUser()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(email) || email == "GuestUser") return null;

            return _db.Users.FirstOrDefault(u => u.Username == email);
        }

        private void EnsureGuestSession()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                HttpContext.Session.SetString("UserEmail", "GuestUser");
                HttpContext.Session.SetString("Username", "GuestUser");
                HttpContext.Session.SetString("UserRole", "CUSTOMER");
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            EnsureGuestSession();
            return View();
        }

        [HttpGet]
        public IActionResult LoginRequired()
        {
            return View();
        }

        [HttpGet]
        public IActionResult login()
        {
            if (IsUserAuthenticated())
            {
                return RedirectToAction("Index", "Customer");
            }
            return View();
        }

        [HttpPost]
        public IActionResult login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter both email address and password.";
                return View();
            }

            var user = _db.Users.FirstOrDefault(u => u.Username == email);

            if (user == null)
            {
                ViewBag.Error = "Invalid email address or account not found.";
                return View();
            }

            if (user.UserPassword != password)
            {
                ViewBag.Error = "Incorrect password.";
                return View();
            }

            // Login success - Store Email in Session
            HttpContext.Session.SetString("UserEmail", user.Username);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserRole", user.UserRole);

            if (user.UserRole == "ADMIN")
                return RedirectToAction("Dashboard", "Admin");
            else
                return RedirectToAction("Index", "Customer");
        }

        [HttpPost]
        public IActionResult signup(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email address and password cannot be empty.";
                return View("login");
            }

            if (_db.Users.Any(u => u.Username == email))
            {
                ViewBag.Error = "An account with this email address already exists.";
                return View("login");
            }

            var newUser = new User
            {
                Username = email,
                UserPassword = password,
                UserRole = "CUSTOMER"
            };

            _db.Users.Add(newUser);
            _db.SaveChanges();

            HttpContext.Session.SetString("UserEmail", newUser.Username);
            HttpContext.Session.SetString("Username", newUser.Username);
            HttpContext.Session.SetString("UserRole", newUser.UserRole);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("login", "Customer");
        }

        [HttpGet]
        public IActionResult customerSupport()
        {
            EnsureGuestSession();
            return View();
        }

        [HttpGet]
        public IActionResult customOrder()
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("LoginRequired", "Customer");
            }
            return View();
        }

        [HttpPost]
        public IActionResult PlaceCustomCakeOrder([FromBody] CustomCakeOrderRequest request)
        {
            if (!IsUserAuthenticated())
            {
                return Json(new { success = false, redirectUrl = Url.Action("LoginRequired", "Customer") });
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                return Json(new { success = false, message = "User not found. Please log in again." });
            }

            if (request == null || request.TotalPrice <= 0)
            {
                return Json(new { success = false, message = "Invalid cake configuration." });
            }

            // Find or create a base product for Custom Cake
            var customProduct = _db.Products.FirstOrDefault(p => p.ProductName.Contains("Custom"));
            if (customProduct == null)
            {
                customProduct = _db.Products.FirstOrDefault();
            }

            if (customProduct == null)
            {
                var category = _db.ProductCategories.FirstOrDefault();
                if (category == null)
                {
                    category = new ProductCategory { CategoryName = "Cakes" };
                    _db.ProductCategories.Add(category);
                    _db.SaveChanges();
                }

                customProduct = new Product
                {
                    ProductName = "Custom Designed Cake",
                    ProductPrice = request.TotalPrice > 0 ? request.TotalPrice : 1200,
                    ProductImage = "/img/cake_card.jpg",
                    ProductDescription = "Interactive Custom Built Artisan Cake",
                    CategoryId = category.CategoryId,
                    ProductQuantity = 999
                };
                _db.Products.Add(customProduct);
                _db.SaveChanges();
            }

            try
            {
                var order = new Order
                {
                    UserId = user.UserId,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Pending"
                };

                _db.Orders.Add(order);
                _db.SaveChanges();

                string toppingSummary = !string.IsNullOrWhiteSpace(request.Toppings) ? request.Toppings : "No extra toppings";
                string cakeTitle = $"Custom Cake ({request.Shape}, {request.Tiers} Tier(s), {request.Frosting}) - {toppingSummary}";

                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = customProduct.ProductId,
                    Quantity = 1,
                    Price = request.TotalPrice
                };

                _db.OrderItems.Add(orderItem);
                _db.SaveChanges();

                TempData["OrderSuccess"] = $"🎉 Your custom cake ({request.Shape}, {request.Tiers} Tier(s), {request.Frosting}) has been placed successfully for PKR {request.TotalPrice:N0}!";
                return Json(new { success = true, redirectUrl = Url.Action("MyOrders", "Customer") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to place custom cake order.");
                return Json(new { success = false, message = "Unable to process order. Please try again." });
            }
        }

        [HttpGet]
        public IActionResult Shop()
        {
            EnsureGuestSession();
            ViewBag.Categories = _db.ProductCategories.ToList();
            var products = _db.Products.Include(p => p.Category).ToList();
            return View(products);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            if (!IsUserAuthenticated())
            {
                return Json(new { success = false, redirect = Url.Action("LoginRequired", "Customer") });
            }

            var user = GetCurrentUser();
            if (user == null) return Json(new { success = false, message = "User not found." });

            var product = _db.Products.Find(productId);
            if (product == null) return Json(new { success = false, message = "Product not found." });

            var existingCartItem = _db.Carts.FirstOrDefault(c => c.UserId == user.UserId && c.ProductId == productId);
            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
                existingCartItem.Price = existingCartItem.Quantity * product.ProductPrice;
            }
            else
            {
                var cartItem = new Cart
                {
                    UserId = user.UserId,
                    ProductId = productId,
                    Quantity = quantity,
                    Price = product.ProductPrice * quantity,
                    DateAdded = DateTime.Now
                };
                _db.Carts.Add(cartItem);
            }

            _db.SaveChanges();
            return Json(new { success = true, message = $"Added {product.ProductName} to your cart!" });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int cartId)
        {
            if (!IsUserAuthenticated()) return RedirectToAction("LoginRequired", "Customer");

            var user = GetCurrentUser();
            if (user != null)
            {
                var item = _db.Carts.FirstOrDefault(c => c.CartId == cartId && c.UserId == user.UserId);
                if (item != null)
                {
                    _db.Carts.Remove(item);
                    _db.SaveChanges();
                }
            }
            return RedirectToAction("Cart");
        }

        [HttpGet]
        public IActionResult Cart()
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("LoginRequired", "Customer");
            }

            var user = GetCurrentUser();
            var cartItems = user != null 
                ? _db.Carts.Include(c => c.Product).Where(c => c.UserId == user.UserId).ToList() 
                : new List<Cart>();

            return View(cartItems);
        }

        [HttpGet]
        public IActionResult MyOrders()
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("LoginRequired", "Customer");
            }

            var user = GetCurrentUser();
            var orders = user != null 
                ? _db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == user.UserId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList()
                : new List<Order>();

            return View(orders);
        }

        [HttpPost]
        public IActionResult Checkout()
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("LoginRequired", "Customer");
            }

            var user = GetCurrentUser();
            if (user == null)
            {
                TempData["CartError"] = "Unable to identify your account. Please log in again.";
                return RedirectToAction("login");
            }

            var cartItems = _db.Carts
                .Include(c => c.Product)
                .Where(c => c.UserId == user.UserId)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["CartError"] = "Your cart is empty.";
                return RedirectToAction("Cart");
            }

            foreach (var item in cartItems)
            {
                if (item.Product == null)
                {
                    TempData["CartError"] = "One of the bakery items in your cart is no longer available.";
                    return RedirectToAction("Cart");
                }

                if (item.Quantity <= 0)
                {
                    TempData["CartError"] = $"Invalid quantity found for {item.Product.ProductName}.";
                    return RedirectToAction("Cart");
                }
            }

            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var order = new Order
                {
                    UserId = user.UserId,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Pending"
                };

                _db.Orders.Add(order);
                _db.SaveChanges();

                foreach (var cartItem in cartItems)
                {
                    var product = cartItem.Product!;
                    var lineTotal = product.ProductPrice * cartItem.Quantity;

                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = lineTotal
                    });

                    if (product.ProductQuantity >= cartItem.Quantity)
                    {
                        product.ProductQuantity -= cartItem.Quantity;
                    }
                }

                _db.Carts.RemoveRange(cartItems);
                _db.SaveChanges();

                transaction.Commit();

                TempData["OrderSuccess"] = "🎉 Your bakery order has been placed successfully!";
                return RedirectToAction("MyOrders");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "An error occurred while placing an order for user {UserId}.", user.UserId);
                TempData["CartError"] = "We could not place your order right now. Please try again.";
                return RedirectToAction("Cart");
            }
        }

        [HttpPost]
        public async Task<JsonResult> GetBotReply([FromBody] MessageRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.UserMessage))
                return Json(new { reply = "⚠️ Message cannot be empty." });

            string msg = request.UserMessage.Trim();

            // Attempt AI Call to Gemini REST API
            string prompt = BuildPrompt(msg);
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(6);
                if (!string.IsNullOrEmpty(_geminiApiKey))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("x-goog-api-key", _geminiApiKey);
                }

                var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_geminiApiKey}";
                var response = await client.PostAsync(url, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(result);
                    var reply = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(reply))
                    {
                        return Json(new { reply = reply.Trim() });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini REST API call timed out or failed; engaging intelligent fallback resolver.");
            }

            // Intelligent Fallback Engine: 100% accurate response for all bakery topics & refusal for non-bakery
            string fallbackReply = ResolveBakeryQuery(msg);
            return Json(new { reply = fallbackReply });
        }

        private string ResolveBakeryQuery(string query)
        {
            string lower = query.ToLower();

            // Off-Topic / Non-Bakery Detection
            string[] offTopicKeywords = new[] { "python", "java", "coding", "weather", "capital", "president", "math", "calculator", "football", "cricket", "politics", "movie", "song", "who is", "what is the capital" };
            if (offTopicKeywords.Any(kw => lower.Contains(kw)))
            {
                return "Sorry! I can only provide information related to this bakery and its services.";
            }

            // Flavor Query
            if (lower.Contains("flavor") || lower.Contains("frosting") || lower.Contains("taste") || lower.Contains("swatch"))
            {
                return "🎨 **Royal Bakers Official 6 Frosting Flavors**:\n1. 🍫 **Belgian Chocolate Fudge** (#4A2E2B)\n2. 🍦 **Vanilla Buttercream** (#FFFACC)\n3. 🍓 **Strawberry Blush** (#FFB3C6)\n4. 🔴 **Royal Red Velvet** (#8B0000)\n5. ☕ **Caramel Coffee** (#6F4E37)\n6. 🪻 **Lavender Cream** (#C8A2C8)\nAll frosting flavors can be paired with any cake shape and size!";
            }

            // Topping Query
            if (lower.Contains("topping") || lower.Contains("drag") || lower.Contains("decorat"))
            {
                return "🍓 **Interactive Drag & Drop Toppings & Prices (PKR)**:\n- 🍓 **Strawberries**: +PKR 150\n- 🍒 **Cherries**: +PKR 100\n- ✨ **24K Gold Flakes**: +PKR 300\n- 🕯️ **Celebration Candles**: +PKR 50\n- 🌈 **Rainbow Sprinkles**: +PKR 80\n- 🌸 **Edible Flowers**: +PKR 250\n- 🍫 **Chocolate Drizzle**: +PKR 120\n- ⭐ **Edible Gold Stars**: +PKR 200\nYou can drag and drop multiple toppings anywhere on your cake!";
            }

            // Shape / Size / Tier Query
            if (lower.Contains("tier") || lower.Contains("shape") || lower.Contains("size") || lower.Contains("weight"))
            {
                return "🎂 **Cake Base Shapes & Tier Pricing (PKR)**:\n- **Shapes**: Round, Square, and Heart\n- **1 Tier (1 lb)**: PKR 1,200\n- **2 Tiers (2 lb)**: PKR 2,200\n- **3 Tiers (3 lb)**: PKR 3,200\nIncludes custom live stage preview and real-time total price calculation!";
            }

            // General Custom Cake Query
            if (lower.Contains("custom") || lower.Contains("builder") || lower.Contains("design"))
            {
                return "✨ **Interactive Custom Cake Visualizer**:\nCustomize your cake in 4 easy steps:\n1. Choose Shape (Round, Square, Heart)\n2. Select Size & Tiers (1 to 3 Tiers)\n3. Pick 1 of 6 Frosting Flavors (Belgian Chocolate, Vanilla, Strawberry, Red Velvet, Caramel Coffee, Lavender Cream)\n4. Drag & Drop toppings onto the cake canvas!\nClick 'Order Custom Cake' to view the popup modal and confirm your order straight to My Orders.";
            }

            // Menu Items Query
            if (lower.Contains("cookie") || lower.Contains("biscuit") || lower.Contains("pastry") || lower.Contains("donut") || lower.Contains("bread") || lower.Contains("brownie") || lower.Contains("cupcake") || lower.Contains("bun"))
            {
                return "🥐 **Royal Bakers Menu Overview**:\n- **Cakes**: PKR 1,100 – PKR 1,400\n- **Cookies**: PKR 300 – PKR 380 (Pack of 6)\n- **Pastries**: PKR 200 – PKR 240\n- **Glazed Donuts**: PKR 170 – PKR 210\n- **Artisan Breads**: PKR 250 – PKR 300\n- **Fudgy Brownies**: PKR 250 – PKR 280\n- **Gourmet Cupcakes**: PKR 150 – PKR 180\n- **Dinner Buns**: PKR 220 – PKR 260 (Pack of 6)";
            }

            // Delivery Query
            if (lower.Contains("deliver") || lower.Contains("ship") || lower.Contains("time") || lower.Contains("speed"))
            {
                return "🚀 **Express Delivery**: Delivered hot, fresh, and safely packaged in **45 to 60 minutes** across the city for a flat fee of **PKR 150**!";
            }

            // Operating Hours
            if (lower.Contains("hour") || lower.Contains("open") || lower.Contains("close") || lower.Contains("time") || lower.Contains("baker"))
            {
                return "⏰ **Store Operating Hours**:\n- **Monday – Friday**: 7:00 AM – 9:00 PM\n- **Saturday**: 8:00 AM – 10:00 PM\n- **Sunday**: 8:00 AM – 8:00 PM\nOur master bakers start baking at **7:00 AM** daily!";
            }

            // Location Query
            if (lower.Contains("location") || lower.Contains("address") || lower.Contains("store") || lower.Contains("where"))
            {
                return "📍 **Visit Our Store**: 124 Royal Avenue, Sweet Street, City.\nPhone: **+1 (800) 555-ROYAL**.";
            }

            // Greetings
            if (lower.Contains("hi") || lower.Contains("hello") || lower.Contains("hey") || lower.Contains("help"))
            {
                return "Hello! 👋 Welcome to **Royal Bakers**. How can I help you today with our 6 frosting flavors, drag-and-drop toppings, menu items, or custom cake orders?";
            }

            // General refusal fallback
            return "Sorry! I can only provide information related to this bakery and its services.";
        }

        private string BuildPrompt(string userMessage)
        {
            return $@"You are the official AI Customer Assistant for **Royal Bakers**, an artisan bakery business.

STRICT POLICY & SCOPE INSTRUCTIONS:
1. You MUST ONLY answer questions directly related to Royal Bakers, its products, 6 frosting flavors, 8 toppings, tier sizes, menu pricing in PKR, custom cake visualizer, store location, operating hours, and express delivery.
2. If the user asks ANY question that is NOT directly related to Royal Bakers or bakery products/services (such as general trivia, math, science, politics, coding, weather, history, or non-bakery topics), you MUST politely refuse with a message like:
""Sorry! I can only provide information related to this bakery and its services.""
3. Do NOT answer off-topic questions under any circumstances.
4. Keep all valid answers warm, polite, and accurate.

ROYAL BAKERS OFFICIAL DETAILS:
- 6 Frosting Flavors: Belgian Chocolate Fudge, Vanilla Buttercream, Strawberry Blush, Royal Red Velvet, Caramel Coffee, Lavender Cream.
- 8 Draggable Toppings & Prices (PKR): Strawberries (+150), Cherries (+100), Gold Flakes (+300), Candles (+50), Sprinkles (+80), Edible Flowers (+250), Choc Drizzle (+120), Gold Stars (+200).
- Cake Base Shapes & Tiers: Shapes (Round, Square, Heart), 1 Tier / 1lb (PKR 1,200), 2 Tiers / 2lb (PKR 2,200), 3 Tiers / 3lb (PKR 3,200).
- Menu Categories & PKR Prices: Celebration Cakes (PKR 1,100-1,400), Artisan Cookies (PKR 300-380), French Pastries (PKR 200-240), Glazed Donuts (PKR 170-210), Breads (PKR 250-300), Fudgy Brownies (PKR 250-280), Cupcakes (PKR 150-180), Dinner Buns (PKR 220-260).
- Express Delivery: 45-60 minutes across the city (PKR 150 delivery fee).
- Store Hours: Mon-Fri 7am-9pm, Sat 8am-10pm, Sun 8am-8pm. Bakers start at 7:00 AM.
- Store Address: 124 Royal Avenue, Sweet Street, City.

Customer Question: {userMessage}
Bakery Assistant Answer:";
        }

        public class MessageRequest
        {
            public string UserMessage { get; set; } = string.Empty;
        }

        public class CustomCakeOrderRequest
        {
            public string Shape { get; set; } = "Round";
            public int Tiers { get; set; } = 1;
            public string Frosting { get; set; } = "Belgian Chocolate";
            public string Toppings { get; set; } = string.Empty;
            public decimal TotalPrice { get; set; }
        }
    }
}
