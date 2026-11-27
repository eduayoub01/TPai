using System.ComponentModel.DataAnnotations;

namespace TPai.Models;

public class CarCreateDto
{
    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PlateNumber { get; set; } = string.Empty;

    public int CarCategoryId { get; set; }
}

public class CarUpdateDto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PlateNumber { get; set; } = string.Empty;

    public int CarCategoryId { get; set; }
}
