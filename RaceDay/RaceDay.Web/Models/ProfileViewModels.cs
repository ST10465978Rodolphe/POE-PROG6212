using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Web.Models;

// The details a user can change
public class ProfileUpdateModel
{
    [Required(ErrorMessage = "Please enter your first name.")]
    [StringLength(50, ErrorMessage = "First name can be at most 50 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your last name.")]
    [StringLength(50, ErrorMessage = "Last name can be at most 50 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "Phone number can be at most 20 characters.")]
    [Display(Name = "Phone (optional)")]
    public string? Phone { get; set; }
}

// Everything the profile page shows: who the user is (read-only) and the edit form
public class ProfilePageViewModel
{
    public ProfileDto Profile { get; set; } = new();
    public ProfileUpdateModel Form { get; set; } = new();

    // Letters shown in the round avatar when there is no profile picture yet
    public string Initials
    {
        get
        {
            string first = string.IsNullOrWhiteSpace(Profile.FirstName) ? "" : Profile.FirstName.Trim()[..1];
            string last = string.IsNullOrWhiteSpace(Profile.LastName) ? "" : Profile.LastName.Trim()[..1];
            return (first + last).ToUpperInvariant();
        }
    }
}