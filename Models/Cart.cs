using System;
using System.Collections.Generic;

namespace Bakery_Management_System.Models;

public partial class Cart
{
    public int CartId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public DateTime? DateAdded { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
