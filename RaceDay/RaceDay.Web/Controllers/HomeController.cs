using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    // Visitors see the public landing page.
    // Logged-in users are sent straight to the home page of their own role.
    public IActionResult Index()
    {
        var session = HttpContext.Session;

        if (session.IsLoggedIn())
        {
            return session.GetRole() == "Organiser"
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Events", "Participant");
        }

        return View();
    }

    // Friendly page for every error: unknown address (404), a form that expired (400),
    // or an unexpected problem (500). Users never see raw technical messages.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code)
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = code
        });
    }
}