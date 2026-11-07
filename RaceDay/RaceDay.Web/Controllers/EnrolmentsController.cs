using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Organisers see who has entered their events and can confirm or cancel an enrolment.
// (A Participant's own enrolments are shown by the Participant pages.)
[RequireLogin("Organiser")]
public class EnrolmentsController : Controller
{
    private readonly ApiClient _api;

    public EnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    // ---------- All enrolments of one event ----------

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
            TempData["Error"] = "You can only view the enrolments of your own events.";
            return RedirectToAction("Events", "Organiser");
        }

        var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/enrolments/event/{eventId}");

        if (enrolments.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!enrolments.Success)
        {
            TempData["Error"] = enrolments.Error;
        }

        return View(new EventEnrolmentsViewModel
        {
            Event = ev.Data,
            Enrolments = enrolments.Data ?? new List<EnrolmentDto>()
        });
    }

    // ---------- Confirm or cancel an enrolment ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int eventId, int id, string status)
    {
        var result = await _api.PutAsync<EnrolmentDto>(
            $"api/enrolments/{id}/status", new UpdateEnrolmentStatusRequest { Status = status });

        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (result.Success)
        {
            TempData["Success"] = $"The enrolment is now {result.Data?.Status ?? status}.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}