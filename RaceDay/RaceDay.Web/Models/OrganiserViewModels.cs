using RaceDay.Contracts;

namespace RaceDay.Web.Models;

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