using System;
using System.ComponentModel.DataAnnotations;

namespace Bakery_Management_System.Models;

public class CustomCakeOrderRequest
{
    [Required]
    public string Shape { get; set; } = "round";

    [Range(1, 3)]
    public int Tiers { get; set; } = 1;

    [Required]
    public string Frosting { get; set; } = "Belgian Chocolate Fudge";

    public string? Toppings { get; set; }

    [StringLength(60)]
    public string? CakeMessage { get; set; }

    [StringLength(250)]
    public string? SpecialNotes { get; set; }

    [DataType(DataType.Date)]
    public DateTime? NeededBy { get; set; }
}
