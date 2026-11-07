using RaceDay.Contracts;

namespace RaceDay.Web.Models;

// Everything the enrolments page of one event shows
public class EventEnrolmentsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();

    public int ConfirmedCount => Enrolments.Count(e => e.Status == "Confirmed");
    public int CancelledCount => Enrolments.Count(e => e.Status == "Cancelled");
}