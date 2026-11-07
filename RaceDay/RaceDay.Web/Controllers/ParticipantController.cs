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

        if (categories.StatusCode == HttpStatusCode.Unauthorized)
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
                .ToList()
        });
    }

    // ---------- Placeholders (built in the next steps) ----------

    public IActionResult MyEvents() => View("ComingSoon", "My Events");

    public IActionResult MyResults() => View("ComingSoon", "My Results");
}