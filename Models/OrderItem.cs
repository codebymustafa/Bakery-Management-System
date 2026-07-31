using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bakery_Management_System.Models;

public partial class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    [NotMapped]
    public string? CustomCakeShape { get; set; }

    [NotMapped]
    public int? CustomCakeTiers { get; set; }

    [NotMapped]
    public string? CustomCakeFrosting { get; set; }

    [NotMapped]
    public string? CustomCakeToppings { get; set; }

    [NotMapped]
    public string? CustomCakeMessage { get; set; }

    [NotMapped]
    public string? CustomCakeNotes { get; set; }

    [NotMapped]
    public DateTime? CustomCakeNeededBy { get; set; }

    [NotMapped]
    public bool IsCustomCake => !string.IsNullOrWhiteSpace(CustomCakeShape);

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
