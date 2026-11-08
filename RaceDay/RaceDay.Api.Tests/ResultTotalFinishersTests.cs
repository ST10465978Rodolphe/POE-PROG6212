using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class ResultTotalFinishersTests : ApiTestBase
{
    public ResultTotalFinishersTests(RaceDayFactory factory) : base(factory)
    {
    }

    private static async Task<EnrolmentDto> EnrolAndRead(HttpClient participant, int eventId, int categoryId)
    {
        var response = await Enrol(participant, eventId, categoryId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }

    private static async Task RecordResult(HttpClient organiser, int enrolmentId, int minutes, int position)
    {
        var response = await organiser.PostAsJsonAsync("/api/results", new CreateResultRequest
        {
            EnrolmentId = enrolmentId,
            FinishTime = new TimeSpan(0, minutes, 0),
            Position = position
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task MyResults_IncludeEventDateAndTotalFinishersOfTheCategory()
    {
        var (organiser, ev, category) = await NewEventWithCategory();
        var first = await LoggedInClient("Participant");
        var second = await LoggedInClient("Participant");

        var firstEnrolment = await EnrolAndRead(first, ev.EventId, category.CategoryId);
        var secondEnrolment = await EnrolAndRead(second, ev.EventId, category.CategoryId);

        await RecordResult(organiser, firstEnrolment.EnrolmentId, minutes: 50, position: 1);
        await RecordResult(organiser, secondEnrolment.EnrolmentId, minutes: 55, position: 2);

        var mine = await second.GetFromJsonAsync<List<ResultDto>>("/api/results/mine");

        var result = Assert.Single(mine!);
        Assert.Equal(2, result.Position);
        Assert.Equal(2, result.TotalFinishers);      // "2nd out of 2 finishers"
        Assert.Equal(ev.EventDate, result.EventDate);
    }
}