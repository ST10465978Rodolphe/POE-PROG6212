using RaceDay.Contracts;

namespace RaceDay.Web.Models;

// The "Browse Events" page: upcoming events and the filters that were used
public class EventBrowseViewModel
{
    public string? Search { get; set; }
    public string? Type { get; set; }

    // How many upcoming events exist before any filter is applied
    public int TotalUpcoming { get; set; }

    public List<EventDto> Events { get; set; } = new();

    public bool IsFiltered =>
        !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Type);
}

// The page with the full information about one event
public class EventDetailsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();

    // The logged-in Participant's active (confirmed) enrolment in this event, if any
    public EnrolmentDto? Enrolment { get; set; }

    public bool HasPassed => Event.EventDate < DateOnly.FromDateTime(DateTime.Today);

    // The "Enter this event" form is shown only when it makes sense
    public bool CanEnrol => !HasPassed && Enrolment == null && Categories.Any();
}