using EventHub.Domin.Constants;
using EventHub.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace EventHub.MVC.Controllers;

public sealed class HomeController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => View();

    [Authorize]
    [HttpGet]
    public IActionResult Authenticated()
    {
        if (User.IsInRole(RoleNames.Admin))
            return RedirectToAction("Dashboard", "Admin");
        if (User.IsInRole(RoleNames.Organizer))
            return RedirectToAction("Dashboard", "Organizer");
        if (User.IsInRole(RoleNames.Attendee))
            return RedirectToAction("My", "Registrations");

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpGet]
    public IActionResult Admin() => View("RoleAccess", "Administrator");

    [Authorize(Roles = RoleNames.Organizer)]
    [HttpGet]
    public IActionResult Organizer() => View("RoleAccess", "Organizer");

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View(new ErrorViewModel { RequestId = requestId });
    }

    [AllowAnonymous]
    [HttpGet]
    [ActionName("StatusCode")]
    public IActionResult HttpStatusCode(int statusCode)
    {
        Response.StatusCode = statusCode;
        return View(statusCode);
    }
}
