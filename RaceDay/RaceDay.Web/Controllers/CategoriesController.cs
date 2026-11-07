using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Organisers manage the categories (age groups or distances) of their own events.
// All data comes from the API, which also enforces who may change what.
[RequireLogin("Organiser")]
public class CategoriesController : Controller
{
    private readonly ApiClient _api;

    public CategoriesController(ApiClient api)
    {
        _api = api;
    }

    // ---------- List the categories of an event (and the "add" form) ----------

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var (page, redirect) = await LoadPageAsync(eventId, new CategoryFormViewModel());
        if (redirect != null)
        {
            return redirect;
        }

        return View(page);
    }

    // ---------- Add a category ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int eventId, CategoryFormViewModel form)
    {
        string? error = null;

        if (ModelState.IsValid)
        {
            var result = await _api.PostAsync<CategoryDto>($"api/events/{eventId}/categories", ToRequest(form));

            if (result.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Account");
            }

            if (result.Success)
            {
                TempData["Success"] = "The category was added.";
                return RedirectToAction(nameof(Index), new { eventId });
            }

            // For example: this event already has a category with that name
            error = result.Error;
        }

        // Show the page again with the form as the user filled it in
        var (page, redirect) = await LoadPageAsync(eventId, form);
        if (redirect != null)
        {
            return redirect;
        }

        if (error != null)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View("Index", page);
    }

    // ---------- Edit a category ----------

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var (page, redirect) = await LoadPageAsync(eventId, new CategoryFormViewModel());
        if (redirect != null)
        {
            return redirect;
        }

        var category = page!.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (category == null)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Distance = category.Distance,
            EntryFee = category.EntryFee
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel form)
    {
        form.EventId = eventId;
        form.CategoryId = id;

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _api.PutAsync<CategoryDto>($"api/categories/{id}", ToRequest(form));

        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error);
            return View(form);
        }

        TempData["Success"] = "The category was updated.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    // ---------- Delete a category ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await _api.DeleteAsync<MessageResponse>($"api/categories/{id}");

        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (result.Success)
        {
            TempData["Success"] = "The category was deleted.";
        }
        else
        {
            // For example: a category that already has enrolments cannot be deleted
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    // ---------- Helpers ----------

    // Loads the event and its categories. Returns a redirect instead when the page cannot be shown.
    private async Task<(CategoriesPageViewModel? Page, IActionResult? Redirect)> LoadPageAsync(
        int eventId, CategoryFormViewModel form)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}");

        if (ev.StatusCode == HttpStatusCode.Unauthorized)
        {
            return (null, RedirectToAction("Login", "Account"));
        }

        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error;
            return (null, RedirectToAction("Events", "Organiser"));
        }

        // Friendly check for the screen. The API enforces this rule as well.
        if (ev.Data.OrganiserId != HttpContext.Session.GetUserId())
        {
            TempData["Error"] = "You can only manage the categories of your own events.";
            return (null, RedirectToAction("Events", "Organiser"));
        }

        var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");

        if (categories.StatusCode == HttpStatusCode.Unauthorized)
        {
            return (null, RedirectToAction("Login", "Account"));
        }

        if (!categories.Success)
        {
            TempData["Error"] = categories.Error;
        }

        var page = new CategoriesPageViewModel
        {
            Event = ev.Data,
            Categories = categories.Data ?? new List<CategoryDto>(),
            Form = form
        };

        return (page, null);
    }

    // Turns the form into the request the API expects
    private static CreateCategoryRequest ToRequest(CategoryFormViewModel form) => new()
    {
        CategoryName = form.CategoryName.Trim(),
        Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
        Distance = form.Distance!.Value,
        EntryFee = form.EntryFee!.Value
    };
}