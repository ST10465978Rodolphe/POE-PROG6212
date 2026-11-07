using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Organisers record the finish time and finishing position of the participants of their events.
// A recorded result is visible to the participant straight away.
// (A Participant's own results are shown by the Participant pages.)
[RequireLogin("Organiser")]
public class ResultsController : Controller
{
    // Accepted time formats: 0:52:35 or 00:52:35
    private static readonly string[] TimeFormats = { @"h\:mm\:ss", @"hh\:mm\:ss" };

    private readonly ApiClient _api;

    public ResultsController(ApiClient api)
    {
        _api = api;
    }

    // ---------- Results of one event ----------

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");

        if (ev.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error;
            return RedirectToAction("Events", "Organiser");
        }

        // Friendly check for the screen. The API enforces this rule as well.
        if (ev.Data.OrganiserId != HttpContext.Session.GetUserId())
        {
            TempData["Error"] = "You can only manage the results of your own events.";
            return RedirectToAction("Events", "Organiser");
        }

        var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/enrolments/event/{eventId}");
        var results = await _api.GetAsync<List<ResultDto>>($"api/results/event/{eventId}");

        if (enrolments.StatusCode == HttpStatusCode.Unauthorized ||
            results.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!enrolments.Success)
        {
            TempData["Error"] = enrolments.Error;
        }
        else if (!results.Success)
        {
            TempData["Error"] = results.Error;
        }

        var resultsByEnrolment = (results.Data ?? new List<ResultDto>())
            .ToDictionary(r => r.EnrolmentId);

        // Only confirmed enrolments can have a result
        var rows = (enrolments.Data ?? new List<EnrolmentDto>())
            .Where(e => e.Status == "Confirmed")
            .Select(e => new ResultRow
            {
                Enrolment = e,
                Result = resultsByEnrolment.GetValueOrDefault(e.EnrolmentId)
            })
            .OrderBy(r => r.Enrolment.CategoryName)
            .ThenBy(r => r.Result == null)                 // recorded results first
            .ThenBy(r => r.Result?.Position ?? int.MaxValue)
            .ThenBy(r => r.Enrolment.ParticipantName)
            .ToList();

        return View(new EventResultsViewModel { Event = ev.Data, Rows = rows });
    }

    // ---------- Record a result ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(int eventId, int enrolmentId, string? finishTime, int? position)
    {
        if (string.IsNullOrWhiteSpace(finishTime) ||
            !TimeSpan.TryParseExact(finishTime.Trim(), TimeFormats, CultureInfo.InvariantCulture, out var time) ||
            time <= TimeSpan.Zero)
        {
            TempData["Error"] = "Please enter the finish time as hours:minutes:seconds, for example 0:52:35.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (position == null || position < 1)
        {
            TempData["Error"] = "Please enter the finishing position as a whole number, 1 or higher.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var result = await _api.PostAsync<ResultDto>("api/results", new CreateResultRequest
        {
            EnrolmentId = enrolmentId,
            FinishTime = time,
            Position = position.Value
        });

        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (result.Success)
        {
            TempData["Success"] = $"The result for {result.Data?.ParticipantName ?? "the participant"} was saved.";
        }
        else
        {
            // For example: a result was already recorded for this enrolment
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}