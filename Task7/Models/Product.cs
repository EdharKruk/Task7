using System.ComponentModel.DataAnnotations;

namespace Task7.Models;

public class Product
{
    [Required]
    public int IdProduct { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    public decimal Price { get; set; }
}