using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Web.Models;

// ---------- Dashboard ----------

// One row of the dashboard: an event, how many people are enrolled, and whether it is still to come
public class EventSummary
{
    public EventDto Event { get; set; } = new();
    public int Enrolments { get; set; }
    public bool IsUpcoming { get; set; }
}

public class DashboardViewModel
{
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public List<EventSummary> Events { get; set; } = new();
}

// ---------- Create / edit an event ----------

public class EventFormViewModel
{
    // 0 when creating a new event
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(100, ErrorMessage = "The event name can be at most 100 characters.")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "The description can be at most 255 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Event date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150, ErrorMessage = "The location can be at most 150 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in kilometres.")]
    [Range(0.01, 1000, ErrorMessage = "The distance must be between 0.01 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? Distance { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = string.Empty;

    // The banner already saved for this event (shown when editing)
    public string? BannerUrl { get; set; }

    // A new banner image chosen in the form (optional)
    [Display(Name = "Event banner image")]
    public IFormFile? Banner { get; set; }
}