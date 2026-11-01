using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Every page in this controller is for Organisers only.
// All data comes from the API: this controller only asks for it and shows it.
[RequireLogin("Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiClient _api;

    public OrganiserController(ApiClient api)
    {
        _api = api;
    }

    // ---------- Dashboard ----------

    public async Task<IActionResult> Dashboard()
    {
        var events = await _api.GetAsync<List<EventDto>>("api/events/mine");

        // The API session ended (for example it expired): log in again
        if (events.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!events.Success)
        {
            TempData["Error"] = events.Error;
            return View(new DashboardViewModel());
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var model = new DashboardViewModel();

        // Upcoming events first (soonest first), then past events
        var ordered = (events.Data ?? new List<EventDto>())
            .OrderBy(e => e.EventDate < today)
            .ThenBy(e => e.EventDate);

        foreach (var ev in ordered)
        {
            // Count the confirmed enrolments of this event
            var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/enrolments/event/{ev.EventId}");
            int count = enrolments.Data?.Count(e => e.Status == "Confirmed") ?? 0;

            model.Events.Add(new EventSummary
            {
                Event = ev,
                Enrolments = count,
                IsUpcoming = ev.EventDate >= today
            });
        }

        model.TotalEvents = model.Events.Count;
        model.UpcomingEvents = model.Events.Count(e => e.IsUpcoming);
        model.TotalEnrolments = model.Events.Sum(e => e.Enrolments);

        return View(model);
    }

    // ---------- Placeholder (built in the next step) ----------

    public IActionResult Events() => View("ComingSoon", "My Events");
}