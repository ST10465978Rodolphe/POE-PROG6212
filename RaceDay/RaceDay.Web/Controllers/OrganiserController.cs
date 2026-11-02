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

    // ---------- My events ----------

    public async Task<IActionResult> Events()
    {
        var events = await _api.GetAsync<List<EventDto>>("api/events/mine");

        if (events.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!events.Success)
        {
            TempData["Error"] = events.Error;
            return View(new List<EventDto>());
        }

        return View(events.Data ?? new List<EventDto>());
    }

    // ---------- Create an event ----------

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var created = await _api.PostAsync<EventDto>("api/events", ToRequest(model));

        if (created.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!created.Success || created.Data == null)
        {
            ModelState.AddModelError(string.Empty, created.Error);
            return View("EventForm", model);
        }

        // The banner is optional. The file goes to the API, which stores it in Azure.
        if (model.Banner != null && model.Banner.Length > 0)
        {
            var upload = await _api.UploadAsync<UploadResponse>(
                $"api/events/{created.Data.EventId}/banner", model.Banner);

            if (!upload.Success)
            {
                TempData["Error"] = $"The event was created, but the banner could not be saved: {upload.Error}";
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = "Your event was created.";
        return RedirectToAction(nameof(Events));
    }

    // ---------- Edit an event ----------

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");

        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Events));
        }

        // Friendly check for the screen. The API enforces this rule as well.
        if (result.Data.OrganiserId != HttpContext.Session.GetUserId())
        {
            TempData["Error"] = "You can only edit your own events.";
            return RedirectToAction(nameof(Events));
        }

        return View("EventForm", ToForm(result.Data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.EventId = id;

        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        var updated = await _api.PutAsync<EventDto>($"api/events/{id}", ToRequest(model));

        if (updated.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error);
            return View("EventForm", model);
        }

        // A new banner replaces the old one (the API removes the old image from Azure)
        if (model.Banner != null && model.Banner.Length > 0)
        {
            var upload = await _api.UploadAsync<UploadResponse>($"api/events/{id}/banner", model.Banner);

            if (!upload.Success)
            {
                TempData["Error"] = $"Your changes were saved, but the banner could not be replaced: {upload.Error}";
                return RedirectToAction(nameof(EditEvent), new { id });
            }
        }

        TempData["Success"] = "Your event was updated.";
        return RedirectToAction(nameof(Events));
    }

    // ---------- Delete an event ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await _api.DeleteAsync<MessageResponse>($"api/events/{id}");

        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (result.Success)
        {
            TempData["Success"] = "The event was deleted.";
        }
        else
        {
            // For example: an event that already has enrolments cannot be deleted
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Events));
    }

    // ---------- Helpers ----------

    // Turns the form into the request the API expects
    private static CreateEventRequest ToRequest(EventFormViewModel model) => new()
    {
        EventName = model.EventName.Trim(),
        Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
        EventDate = model.EventDate!.Value,
        Location = model.Location.Trim(),
        Distance = model.Distance!.Value,
        EventType = model.EventType
    };

    // Fills the form with an existing event
    private static EventFormViewModel ToForm(EventDto ev) => new()
    {
        EventId = ev.EventId,
        EventName = ev.EventName,
        Description = ev.Description,
        EventDate = ev.EventDate,
        Location = ev.Location,
        Distance = ev.Distance,
        EventType = ev.EventType,
        BannerUrl = ev.BannerUrl
    };
}