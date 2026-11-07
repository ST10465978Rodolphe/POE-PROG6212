using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Web.Models;

// The add / edit category form
public class CategoryFormViewModel
{
    // Set when editing an existing category (0 when adding)
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the category name, for example Under 20 or 10 KM Open.")]
    [StringLength(100, ErrorMessage = "The category name can be at most 100 characters.")]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "The description can be at most 255 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please enter the distance in kilometres.")]
    [Range(0.01, 1000, ErrorMessage = "The distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Please enter the entry fee. Use 0 for free entry.")]
    [Range(0, 100000, ErrorMessage = "The entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }
}

// Everything the categories page shows: the event, its categories and the "add" form
public class CategoriesPageViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}