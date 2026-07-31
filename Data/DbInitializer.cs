using System;
using System.Collections.Generic;
using System.Linq;
using Bakery_Management_System.Models;

namespace Bakery_Management_System.Data
{
    public static class DbInitializer
    {
        public static void Initialize(BakeryDbContext context)
        {
            try
            {
                context.Database.EnsureCreated();

                // Seed categories if missing
                var requiredCategories = new[] { "Cakes", "Cookies", "Breads", "Pastries", "Donuts", "Cupcakes", "Brownies", "Buns" };
                foreach (var catName in requiredCategories)
                {
                    if (!context.ProductCategories.Any(c => c.CategoryName == catName))
                    {
                        context.ProductCategories.Add(new ProductCategory { CategoryName = catName });
                    }
                }
                context.SaveChanges();

                // Seed products if catalog has fewer than 8 items
                if (!context.Products.Any())
                {
                    var catMap = context.ProductCategories.ToDictionary(c => c.CategoryName, c => c.CategoryId);

                    int cakesCatId = catMap.ContainsKey("Cakes") ? catMap["Cakes"] : context.ProductCategories.First().CategoryId;
                    int cookiesCatId = catMap.ContainsKey("Cookies") ? catMap["Cookies"] : cakesCatId;
                    int breadsCatId = catMap.ContainsKey("Breads") ? catMap["Breads"] : cakesCatId;
                    int pastriesCatId = catMap.ContainsKey("Pastries") ? catMap["Pastries"] : cakesCatId;
                    int donutsCatId = catMap.ContainsKey("Donuts") ? catMap["Donuts"] : cakesCatId;
                    int cupcakesCatId = catMap.ContainsKey("Cupcakes") ? catMap["Cupcakes"] : cakesCatId;
                    int browniesCatId = catMap.ContainsKey("Brownies") ? catMap["Brownies"] : cakesCatId;
                    int bunsCatId = catMap.ContainsKey("Buns") ? catMap["Buns"] : cakesCatId;

                    var products = new List<Product>
                    {
                        // Custom Cake Base Product
                        new Product { ProductName = "Custom Designed Cake", ProductPrice = 1200, ProductImage = "/img/cake_card.jpg", ProductDescription = "Interactive Custom Built Artisan Cake.", CategoryId = cakesCatId, ProductQuantity = 999 },

                        // Cakes
                        new Product { ProductName = "Royal Chocolate Truffle Cake", ProductPrice = 1200, ProductImage = "/img/cake_card.jpg", ProductDescription = "Rich belgian chocolate sponge layered with dark chocolate ganache.", CategoryId = cakesCatId, ProductQuantity = 20 },
                        new Product { ProductName = "Red Velvet Supreme Cake", ProductPrice = 1400, ProductImage = "/img/cake_card.jpg", ProductDescription = "Classic red velvet with silky cream cheese frosting.", CategoryId = cakesCatId, ProductQuantity = 15 },
                        new Product { ProductName = "Vanilla Caramel Crunch Cake", ProductPrice = 1100, ProductImage = "/img/cake_card.jpg", ProductDescription = "Fluffy vanilla sponge with salted caramel drizzle and crunch.", CategoryId = cakesCatId, ProductQuantity = 18 },
                        new Product { ProductName = "Dark Fudge Black Forest Cake", ProductPrice = 1300, ProductImage = "/img/cake_card.jpg", ProductDescription = "Moist cocoa cake with cherry filling and whipped cream.", CategoryId = cakesCatId, ProductQuantity = 12 },

                        // Cookies
                        new Product { ProductName = "Choco Chip Crisp Cookies (Pack of 6)", ProductPrice = 350, ProductImage = "/img/cookies_card.jpg", ProductDescription = "Golden crispy edges with melting chocolate chips inside.", CategoryId = cookiesCatId, ProductQuantity = 50 },
                        new Product { ProductName = "Butter Shortbread Cookies (Pack of 6)", ProductPrice = 300, ProductImage = "/img/cookies_card.jpg", ProductDescription = "Melt-in-mouth buttery shortbread biscuits.", CategoryId = cookiesCatId, ProductQuantity = 40 },
                        new Product { ProductName = "Double Dark Chocolate Cookies (Pack of 6)", ProductPrice = 380, ProductImage = "/img/cookies_card.jpg", ProductDescription = "Rich cocoa cookies loaded with dark chocolate chunks.", CategoryId = cookiesCatId, ProductQuantity = 35 },
                        new Product { ProductName = "Oatmeal Honey Raisin Cookies (Pack of 6)", ProductPrice = 320, ProductImage = "/img/cookies_card.jpg", ProductDescription = "Healthy oats baked with pure honey and sweet raisins.", CategoryId = cookiesCatId, ProductQuantity = 30 },

                        // Breads
                        new Product { ProductName = "Artisan Sourdough Loaf", ProductPrice = 300, ProductImage = "/img/bread_card.jpg", ProductDescription = "Traditional slow-fermented artisan sourdough bread.", CategoryId = breadsCatId, ProductQuantity = 25 },
                        new Product { ProductName = "Crusty French Baguette", ProductPrice = 250, ProductImage = "/img/bread_card.jpg", ProductDescription = "Golden crispy crust with soft airy center.", CategoryId = breadsCatId, ProductQuantity = 30 },
                        new Product { ProductName = "Whole Wheat Country Bread", ProductPrice = 280, ProductImage = "/img/bread_card.jpg", ProductDescription = "Nutritious whole grain loaf baked fresh every morning.", CategoryId = breadsCatId, ProductQuantity = 20 },
                        new Product { ProductName = "Soft Honey Milk Bread", ProductPrice = 260, ProductImage = "/img/bread_card.jpg", ProductDescription = "Sweet fluffy sandwich bread baked with milk and honey.", CategoryId = breadsCatId, ProductQuantity = 25 },

                        // Pastries
                        new Product { ProductName = "French Butter Croissant", ProductPrice = 200, ProductImage = "/img/pastries_card.jpg", ProductDescription = "Flaky French pastry baked with 100% organic butter.", CategoryId = pastriesCatId, ProductQuantity = 40 },
                        new Product { ProductName = "Strawberry Cream Puff Pastry", ProductPrice = 220, ProductImage = "/img/pastries_card.jpg", ProductDescription = "Light choux pastry filled with fresh strawberry cream.", CategoryId = pastriesCatId, ProductQuantity = 30 },
                        new Product { ProductName = "Blueberry Cheese Danish", ProductPrice = 240, ProductImage = "/img/pastries_card.jpg", ProductDescription = "Flaky pastry topped with sweet cream cheese and blueberries.", CategoryId = pastriesCatId, ProductQuantity = 25 },
                        new Product { ProductName = "Chocolate Eclair", ProductPrice = 210, ProductImage = "/img/pastries_card.jpg", ProductDescription = "Choux pastry filled with custard and dipped in chocolate.", CategoryId = pastriesCatId, ProductQuantity = 35 },

                        // Donuts
                        new Product { ProductName = "Strawberry Glazed Ring Donut", ProductPrice = 180, ProductImage = "/img/donuts_card.jpg", ProductDescription = "Soft fried ring donut with sweet strawberry frosting.", CategoryId = donutsCatId, ProductQuantity = 45 },
                        new Product { ProductName = "Chocolate Fudge Dip Donut", ProductPrice = 190, ProductImage = "/img/donuts_card.jpg", ProductDescription = "Fluffy donut coated in thick chocolate fudge glaze.", CategoryId = donutsCatId, ProductQuantity = 40 },
                        new Product { ProductName = "Boston Custard Cream Donut", ProductPrice = 210, ProductImage = "/img/donuts_card.jpg", ProductDescription = "Filled donut stuffed with vanilla custard cream.", CategoryId = donutsCatId, ProductQuantity = 30 },
                        new Product { ProductName = "Cinnamon Sugar Ring Donut", ProductPrice = 170, ProductImage = "/img/donuts_card.jpg", ProductDescription = "Warm fried donut dusted with aromatic cinnamon sugar.", CategoryId = donutsCatId, ProductQuantity = 35 },

                        // Cupcakes
                        new Product { ProductName = "Vanilla Swirl Gourmet Cupcake", ProductPrice = 150, ProductImage = "/img/cupcakes_card.jpg", ProductDescription = "Fluffy vanilla cupcake topped with whipped buttercream.", CategoryId = cupcakesCatId, ProductQuantity = 50 },
                        new Product { ProductName = "Velvet Cream Cheese Cupcake", ProductPrice = 170, ProductImage = "/img/cupcakes_card.jpg", ProductDescription = "Mini red velvet cupcake topped with cream cheese swirl.", CategoryId = cupcakesCatId, ProductQuantity = 40 },
                        new Product { ProductName = "Dark Chocolate Fudge Cupcake", ProductPrice = 160, ProductImage = "/img/cupcakes_card.jpg", ProductDescription = "Rich cocoa cupcake topped with chocolate buttercream.", CategoryId = cupcakesCatId, ProductQuantity = 45 },
                        new Product { ProductName = "Salted Caramel Cupcake", ProductPrice = 180, ProductImage = "/img/cupcakes_card.jpg", ProductDescription = "Caramel sponge topped with salted caramel buttercream.", CategoryId = cupcakesCatId, ProductQuantity = 35 },

                        // Brownies
                        new Product { ProductName = "Fudgy Dark Walnut Brownie", ProductPrice = 250, ProductImage = "/img/brownies_card.jpg", ProductDescription = "Melt-in-mouth fudgy cocoa square packed with walnuts.", CategoryId = browniesCatId, ProductQuantity = 40 },
                        new Product { ProductName = "Salted Caramel Swirl Brownie", ProductPrice = 270, ProductImage = "/img/brownies_card.jpg", ProductDescription = "Rich chocolate brownie swirled with golden caramel.", CategoryId = browniesCatId, ProductQuantity = 30 },
                        new Product { ProductName = "Triple Chocolate Chunk Brownie", ProductPrice = 280, ProductImage = "/img/brownies_card.jpg", ProductDescription = "Loaded with milk, dark, and white chocolate chunks.", CategoryId = browniesCatId, ProductQuantity = 35 },
                        new Product { ProductName = "Peanut Butter Fudge Brownie", ProductPrice = 260, ProductImage = "/img/brownies_card.jpg", ProductDescription = "Decadent dark chocolate brownie with peanut butter layer.", CategoryId = browniesCatId, ProductQuantity = 25 },

                        // Buns
                        new Product { ProductName = "Golden Soft Dinner Buns (Pack of 6)", ProductPrice = 220, ProductImage = "/img/buns_card.jpg", ProductDescription = "Freshly baked soft dinner rolls perfect with tea or soup.", CategoryId = bunsCatId, ProductQuantity = 40 },
                        new Product { ProductName = "Sweet Honey Butter Buns (Pack of 6)", ProductPrice = 240, ProductImage = "/img/buns_card.jpg", ProductDescription = "Soft fluffy buns brushed with sweet honey butter glaze.", CategoryId = bunsCatId, ProductQuantity = 30 },
                        new Product { ProductName = "Garlic Cheese Herb Buns (Pack of 6)", ProductPrice = 260, ProductImage = "/img/buns_card.jpg", ProductDescription = "Savory buns infused with garlic butter and melted cheese.", CategoryId = bunsCatId, ProductQuantity = 25 },
                        new Product { ProductName = "Cinnamon Raisin Sweet Buns (Pack of 6)", ProductPrice = 250, ProductImage = "/img/buns_card.jpg", ProductDescription = "Spiced sweet buns packed with raisins and cinnamon frosting.", CategoryId = bunsCatId, ProductQuantity = 20 }
                    };

                    context.Products.AddRange(products);
                    context.SaveChanges();
                }

                // Seed default admin (admin@gmail.com / admin123) without duplicates
                if (!context.Users.Any(u => u.Username == "admin@gmail.com"))
                {
                    context.Users.Add(new User
                    {
                        Username = "admin@gmail.com",
                        UserPassword = "admin123",
                        UserRole = "ADMIN"
                    });
                    context.SaveChanges();
                }

                // Seed default customer if Users table is completely empty
                if (!context.Users.Any(u => u.Username == "customer@royalbakers.com"))
                {
                    context.Users.Add(new User
                    {
                        Username = "customer@royalbakers.com",
                        UserPassword = "customerpassword",
                        UserRole = "CUSTOMER"
                    });
                    context.SaveChanges();
                }
            }
            catch (Exception)
            {
                // Silently handle seed logging
            }
        }
    }
}
