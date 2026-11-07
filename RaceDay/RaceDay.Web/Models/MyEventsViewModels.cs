using RaceDay.Contracts;

namespace RaceDay.Web.Models;

// One line of "My Events": an enrolment and the event it belongs to
public class MyEventRow
{
    public EnrolmentDto Enrolment { get; set; } = new();

    // Date, place and so on. Null only if the event details could not be loaded.
    public EventDto? Event { get; set; }
}

public class MyEventsViewModel
{
    public List<MyEventRow> Upcoming { get; set; } = new();
    public List<MyEventRow> Past { get; set; } = new();

    public bool IsEmpty => !Upcoming.Any() && !Past.Any();
}