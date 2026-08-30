using EventHub.Domin.Constants;
using EventHub.MVC.Models;
using EventHub.MVC.Models.Registrations;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Registrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.MVC.Controllers;

[Authorize(Roles = RoleNames.Attendee)]
[Route("Registrations")]
public sealed class RegistrationsController(IRegistrationApiClient registrationApiClient) : Controller
{
    [HttpPost("Create/{eventId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await registrationApiClient.RegisterAsync(eventId, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        if (string.IsNullOrWhiteSpace(result.Data.PaymentUrl))
        {
            TempData["Success"] = "Your registration has been confirmed.";
            return RedirectToAction(nameof(My));
        }

        return Redirect(result.Data.PaymentUrl);
    }

    [HttpGet("My")]
    public async Task<IActionResult> My(int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var result = await registrationApiClient.GetMyAsync(Math.Max(pageNumber, 1), 10, cancellationToken);
        return View(new MyRegistrationsViewModel
        {
            Registrations = result.Data ?? new(),
            ErrorMessage = result.IsSuccess ? null : result.Message
        });
    }

    [HttpGet("PaymentReturn/{registrationId:guid}")]
    public async Task<IActionResult> PaymentReturn(Guid registrationId, CancellationToken cancellationToken)
    {
        // Paymob query parameters are deliberately ignored. The API status is webhook-authoritative.
        var result = await registrationApiClient.GetStatusAsync(registrationId, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
            return result.FailureKind == ApiFailureKind.NotFound
                ? NotFound()
                : View("~/Views/Home/Error.cshtml", new ErrorViewModel());

        return View(result.Data);
    }

    [HttpPost("{registrationId:guid}/Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid registrationId, CancellationToken cancellationToken)
    {
        var result = await registrationApiClient.CancelAsync(registrationId, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Your registration has been canceled."
            : result.Message;
        return RedirectToAction(nameof(My));
    }
}
