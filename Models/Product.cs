using System;
using System.Collections.Generic;

namespace Bakery_Management_System.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public decimal ProductPrice { get; set; }

    public string? ProductImage { get; set; }

    public string? ProductDescription { get; set; }

    public int? CategoryId { get; set; }

    public int ProductQuantity { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ProductCategory? Category { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
