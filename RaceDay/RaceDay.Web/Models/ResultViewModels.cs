using RaceDay.Contracts;

namespace RaceDay.Web.Models;

// One line of the results page: a confirmed enrolment and its result (if already recorded)
public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class EventResultsViewModel
{
    public EventDto Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();

    public int RecordedCount => Rows.Count(r => r.Result != null);
    public int WaitingCount => Rows.Count(r => r.Result == null);

    // Results are normally entered once the event has taken place
    public bool EventIsInTheFuture => Event.EventDate > DateOnly.FromDateTime(DateTime.Today);
}