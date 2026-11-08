using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Both roles can view and update their own profile and upload a profile picture.
// The picture goes to the API, which stores it in Azure: this site never talks to Azure.
[RequireLogin]
public class ProfileController : Controller
{
    private readonly ApiClient _api;

    public ProfileController(ApiClient api)
    {
        _api = api;
    }

    // ---------- View my profile ----------

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (page, redirect) = await LoadAsync();
        if (redirect != null)
        {
            return redirect;
        }

        return View(page);
    }

    // ---------- Update my details ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ProfileUpdateModel form)
    {
        string? error = null;

        if (ModelState.IsValid)
        {
            var result = await _api.PutAsync<ProfileDto>("api/users/me", new UpdateProfileRequest
            {
                FirstName = form.FirstName.Trim(),
                LastName = form.LastName.Trim(),
                Phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim()
            });

            if (result.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Account");
            }

            if (result.Success)
            {
                // The name in the menu comes from the session, so keep it up to date
                HttpContext.Session.SetString(SessionKeys.FullName,
                    $"{form.FirstName.Trim()} {form.LastName.Trim()}");

                TempData["Success"] = "Your profile was updated.";
                return RedirectToAction(nameof(Index));
            }

            error = result.Error;
        }

        // Show the page again; the fields keep what the user typed
        var (page, redirect) = await LoadAsync();
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

    // ---------- Upload my profile picture ----------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPicture(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadAsync<UploadResponse>("api/users/me/profile-picture", file);

        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (result.Success)
        {
            TempData["Success"] = "Your profile picture was updated.";
        }
        else
        {
            // For example: not an image, or larger than 5 MB
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }

    // ---------- Helper ----------

    // Loads the logged-in user's profile from the API
    private async Task<(ProfilePageViewModel? Page, IActionResult? Redirect)> LoadAsync()
    {
        var profile = await _api.GetAsync<ProfileDto>("api/users/me");

        if (profile.StatusCode == HttpStatusCode.Unauthorized)
        {
            return (null, RedirectToAction("Login", "Account"));
        }

        if (!profile.Success || profile.Data == null)
        {
            TempData["Error"] = profile.Error;
            return (null, RedirectToAction("Index", "Home"));
        }

        var page = new ProfilePageViewModel
        {
            Profile = profile.Data,
            Form = new ProfileUpdateModel
            {
                FirstName = profile.Data.FirstName,
                LastName = profile.Data.LastName,
                Phone = profile.Data.Phone
            }
        };

        return (page, null);
    }
}