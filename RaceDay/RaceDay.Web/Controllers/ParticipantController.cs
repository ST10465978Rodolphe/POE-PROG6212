using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Every page in this controller is for Participants only.
// All data comes from the API: this controller only asks for it and shows it.
[RequireLogin("Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;

    public ParticipantController(ApiClient api)
    {
        _api = api;
    }

    // ---------- Browse upcoming events (with filters) ----------

    [HttpGet]
    public async Task<IActionResult> Events(string? search, string? type)
    {
        var events = await _api.GetAsync<List<EventDto>>("api/events");

        if (events.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = new EventBrowseViewModel { Search = search, Type = type };

        if (!events.Success)
        {
            TempData["Error"] = events.Error;
            return View(model);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        var upcoming = (events.Data ?? new List<EventDto>())
            .Where(e => e.EventDate >= today)
            .OrderBy(e => e.EventDate)
            .ToList();

        model.TotalUpcoming = upcoming.Count;

        // Filters only change what is shown on the screen
        IEnumerable<EventDto> shown = upcoming;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim();
            shown = shown.Where(e =>
                e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            shown = shown.Where(e => e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        model.Events = shown.ToList();
        return View(model);
    }

    // ---------- Full information about one event ----------

    [HttpGet]
    public async Task<IActionResult> EventDetails(int id)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{id}");

        if (ev.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error;
            return RedirectToAction(nameof(Events));
        }

        var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        var mine = await _api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");

        if (categories.StatusCode == HttpStatusCode.Unauthorized ||
            mine.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!categories.Success)
        {
            TempData["Error"] = categories.Error;
        }

        return View(new EventDetailsViewModel
        {
            Event = ev.Data,
            Categories = (categories.Data ?? new List<CategoryDto>())
                .OrderBy(c => c.Distance)
                .ThenBy(c => c.CategoryName)
                .ToList(),

            // Is this Participant already entered in this event?
            Enrolment = mine.Data?.FirstOrDefault(e => e.EventId == id && e.Status == "Confirmed")
        });
    }

    // ---------- Enter an event ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId == null)
        {
            TempData["Error"] = "Please choose a category to enter.";
            return RedirectToAction(nameof(EventDetails), new { id = eventId });
        }

        // The API records the link between this Participant, the event and the category
        var result = await _api.PostAsync<EnrolmentDto>("api/enrolments", new CreateEnrolmentRequest
        {
            EventId = eventId,
            CategoryId = categoryId.Value
        });

        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (result.Success && result.Data != null)
        {
            TempData["Success"] = $"You are entered in {result.Data.EventName} ({result.Data.CategoryName}).";
        }
        else
        {
            // For example: already enrolled, or the event has already taken place
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(EventDetails), new { id = eventId });
    }

    // ---------- My events: every event I have entered, with its status ----------

    [HttpGet]
    public async Task<IActionResult> MyEvents()
    {
        var mine = await _api.GetAsync<List<EnrolmentDto>>("api/enrolments/mine");
        var events = await _api.GetAsync<List<EventDto>>("api/events");

        if (mine.StatusCode == HttpStatusCode.Unauthorized ||
            events.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!mine.Success)
        {
            TempData["Error"] = mine.Error;
            return View(new MyEventsViewModel());
        }

        if (!events.Success)
        {
            TempData["Error"] = "Some event details could not be loaded. Please try again later.";
        }

        // An enrolment only knows its event by name: look up the date and place
        var eventsById = (events.Data ?? new List<EventDto>()).ToDictionary(e => e.EventId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var rows = (mine.Data ?? new List<EnrolmentDto>())
            .Select(e => new MyEventRow
            {
                Enrolment = e,
                Event = eventsById.GetValueOrDefault(e.EventId)
            })
            .ToList();

        return View(new MyEventsViewModel
        {
            Upcoming = rows
                .Where(r => r.Event == null || r.Event.EventDate >= today)
                .OrderBy(r => r.Event?.EventDate ?? DateOnly.MaxValue)
                .ToList(),

            Past = rows
                .Where(r => r.Event != null && r.Event.EventDate < today)
                .OrderByDescending(r => r.Event!.EventDate)
                .ToList()
        });
    }

    // ---------- My results: my race history ----------

    [HttpGet]
    public async Task<IActionResult> MyResults()
    {
        var results = await _api.GetAsync<List<ResultDto>>("api/results/mine");

        if (results.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!results.Success)
        {
            TempData["Error"] = results.Error;
            return View(new List<ResultDto>());
        }

        // Most recent race first
        return View((results.Data ?? new List<ResultDto>())
            .OrderByDescending(r => r.EventDate)
            .ToList());
    }
}