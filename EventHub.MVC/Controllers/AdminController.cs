using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.MVC.Models;
using EventHub.MVC.Models.Admins;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Organizers;
using EventHub.MVC.Services.Admins;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.MVC.Controllers;

[Authorize(Roles = RoleNames.Admin)]
[Route("Admin")]
public sealed class AdminController(
    IAdminApiClient adminApiClient,
    IEventApiClient eventApiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var result = await adminApiClient.GetDashboardAsync(cancellationToken);
        return View(new AdminDashboardViewModel
        {
            Dashboard = result.Data ?? new(),
            ErrorMessage = result.IsSuccess ? null : result.Message
        });
    }

    [HttpGet("Users")]
    public async Task<IActionResult> Users([FromQuery] AdminUserListFilter filter, CancellationToken cancellationToken)
    {
        filter.PageNumber = Math.Max(filter.PageNumber, 1);
        filter.SearchValue = string.IsNullOrWhiteSpace(filter.SearchValue) ? null : filter.SearchValue.Trim();
        var result = await adminApiClient.GetUsersAsync(filter, cancellationToken);
        return View(new AdminUsersViewModel
        {
            Filter = filter,
            Users = result.Data ?? new(),
            ErrorMessage = result.IsSuccess ? null : result.Message
        });
    }

    [HttpPost("Users/Role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRole(UpdateUserRoleViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "The selected role is invalid.";
            return RedirectToAction(nameof(Users));
        }

        var result = await adminApiClient.UpdateUserRoleAsync(model.UserId, model.Role, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "User role updated successfully."
            : result.Message;
        return RedirectToAction(nameof(Users));
    }

    [HttpGet("Events")]
    public async Task<IActionResult> Events([FromQuery] EventListFilter filter, CancellationToken cancellationToken)
    {
        NormalizeEventFilter(filter);
        var eventsTask = adminApiClient.GetEventsAsync(filter, cancellationToken);
        var categoriesTask = eventApiClient.GetCategoriesAsync(cancellationToken);
        await Task.WhenAll(eventsTask, categoriesTask);

        var eventsResult = await eventsTask;
        var categoriesResult = await categoriesTask;
        return View(new AdminEventListViewModel
        {
            Filter = filter,
            Events = eventsResult.Data ?? new(),
            Categories = categoriesResult.Data ?? [],
            ErrorMessage = eventsResult.IsSuccess ? null : eventsResult.Message
        });
    }

    [HttpGet("Events/Create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = await PopulateCategoriesAsync(new EventEditorViewModel
        {
            EventDate = DateTime.UtcNow.AddHours(1)
        }, cancellationToken);
        return View(model);
    }

    [HttpPost("Events/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventEditorViewModel model, CancellationToken cancellationToken)
    {
        if (!model.CategoryId.HasValue)
            ModelState.AddModelError(nameof(model.CategoryId), "Please select a category.");

        if (!ModelState.IsValid)
            return View(await PopulateCategoriesAsync(model, cancellationToken));

        var result = await adminApiClient.CreateEventAsync(new CreateOrganizerEventRequest(
            model.Title,
            model.Description,
            ToUtc(model.EventDate),
            model.Price,
            model.Location,
            model.CategoryId!.Value,
            model.MaxAttendees,
            model.Mode,
            NormalizeMeetingUrl(model.OnlineMeetingUrl)), cancellationToken);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(await PopulateCategoriesAsync(model, cancellationToken));
        }

        TempData["Success"] = "Event created successfully.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet("Events/{id:guid}/Edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminApiClient.GetEventAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
            return ToReadFailureResult(result);

        if (!result.Data.Status.Equals(nameof(EventStatus.Scheduled), StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only scheduled events can be updated.";
            return RedirectToAction(nameof(Events));
        }

        return View(await PopulateCategoriesAsync(ToEditorModel(result.Data), cancellationToken));
    }

    [HttpPost("Events/{id:guid}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EventEditorViewModel model, CancellationToken cancellationToken)
    {
        model.IsEdit = true;
        if (!ModelState.IsValid)
            return View(await PopulateCategoriesAsync(model, cancellationToken));

        var result = await adminApiClient.UpdateEventAsync(id, new UpdateOrganizerEventRequest(
            id,
            model.Title,
            model.Description,
            ToUtc(model.EventDate),
            model.Location,
            model.CategoryId,
            model.MaxAttendees,
            model.Mode,
            NormalizeMeetingUrl(model.OnlineMeetingUrl)), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.FailureKind is ApiFailureKind.Forbidden or ApiFailureKind.Unauthorized)
                return Forbid();

            ModelState.AddModelError(string.Empty, result.Message);
            return View(await PopulateCategoriesAsync(model, cancellationToken));
        }

        TempData["Success"] = "Event updated successfully.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost("Events/{id:guid}/Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminApiClient.UpdateEventStatusAsync(id, EventStatus.Canceled, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Event canceled successfully. Confirmed attendees will be notified."
            : result.Message;
        return RedirectToAction(nameof(Events));
    }

    [HttpPost("Events/{id:guid}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminApiClient.DeleteEventAsync(id, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Event deleted successfully."
            : result.Message;
        return RedirectToAction(nameof(Events));
    }

    [HttpGet("Events/{id:guid}/Attendees")]
    public async Task<IActionResult> Attendees(Guid id, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var registrationsTask = adminApiClient.GetEventRegistrationsAsync(id, Math.Max(1, pageNumber), 10, cancellationToken);
        var eventTask = adminApiClient.GetEventAsync(id, cancellationToken);
        await Task.WhenAll(registrationsTask, eventTask);

        var registrationsResult = await registrationsTask;
        if (!registrationsResult.IsSuccess)
            return ToReadFailureResult(registrationsResult);

        var eventResult = await eventTask;
        return View(new AdminAttendeesViewModel
        {
            EventId = id,
            EventTitle = eventResult.Data?.Title ?? "Event attendees",
            Attendees = registrationsResult.Data ?? new()
        });
    }

    [HttpGet("Events/{id:guid}/Announcement")]
    public async Task<IActionResult> Announcement(Guid id, CancellationToken cancellationToken)
    {
        var registrationsTask = adminApiClient.GetEventRegistrationsAsync(id, 1, 1, cancellationToken);
        var eventTask = adminApiClient.GetEventAsync(id, cancellationToken);
        await Task.WhenAll(registrationsTask, eventTask);

        var registrationsResult = await registrationsTask;
        if (!registrationsResult.IsSuccess)
            return ToReadFailureResult(registrationsResult);

        var eventResult = await eventTask;
        if (!eventResult.IsSuccess || eventResult.Data is null)
            return ToReadFailureResult(eventResult);

        return View(new EventAnnouncementViewModel
        {
            EventId = id,
            EventTitle = eventResult.Data.Title,
            RegistrationCount = registrationsResult.Data?.TotalCount ?? 0
        });
    }

    [HttpPost("Events/{id:guid}/Announcement")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Announcement(Guid id, EventAnnouncementViewModel model, CancellationToken cancellationToken)
    {
        model.EventId = id;
        if (!ModelState.IsValid)
            return await ReloadAnnouncementAsync(model, cancellationToken);

        var result = await adminApiClient.SendAnnouncementAsync(
            new SendEventAnnouncementRequest(id, model.Subject, model.Message),
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.FailureKind is ApiFailureKind.Forbidden or ApiFailureKind.Unauthorized)
                return Forbid();

            ModelState.AddModelError(string.Empty, result.Message);
            return await ReloadAnnouncementAsync(model, cancellationToken);
        }

        TempData["Success"] = "Announcement queued for confirmed attendees.";
        return RedirectToAction(nameof(Attendees), new { id });
    }

    [HttpGet("Registrations")]
    public async Task<IActionResult> Registrations(int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var result = await adminApiClient.GetRegistrationsAsync(Math.Max(pageNumber, 1), 25, cancellationToken);
        return View(new AdminRegistrationsViewModel
        {
            Registrations = result.Data ?? new(),
            ErrorMessage = result.IsSuccess ? null : result.Message
        });
    }

    private async Task<EventEditorViewModel> PopulateCategoriesAsync(EventEditorViewModel model, CancellationToken cancellationToken)
    {
        var categories = await eventApiClient.GetCategoriesAsync(cancellationToken);
        model.Categories = categories.Data ?? [];
        model.CategoryLoadError = categories.IsSuccess ? null : categories.Message;
        return model;
    }

    private async Task<IActionResult> ReloadAnnouncementAsync(EventAnnouncementViewModel model, CancellationToken cancellationToken)
    {
        var registrationsResult = await adminApiClient.GetEventRegistrationsAsync(model.EventId, 1, 1, cancellationToken);
        if (!registrationsResult.IsSuccess)
            return ToReadFailureResult(registrationsResult);

        var eventResult = await adminApiClient.GetEventAsync(model.EventId, cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null)
            return ToReadFailureResult(eventResult);

        model.EventTitle = eventResult.Data.Title;
        model.RegistrationCount = registrationsResult.Data?.TotalCount ?? 0;
        return View(nameof(Announcement), model);
    }

    private static EventEditorViewModel ToEditorModel(EventDetailsDto eventDetails) => new()
    {
        IsEdit = true,
        Title = eventDetails.Title,
        Description = eventDetails.Description,
        EventDate = ToUtc(eventDetails.EventDate),
        Location = eventDetails.Location,
        MaxAttendees = eventDetails.MaxAttendees,
        Mode = eventDetails.Mode,
        OnlineMeetingUrl = eventDetails.OnlineMeetingUrl,
        Price = (double)eventDetails.Price,
        CurrentCategoryName = eventDetails.CategoryName
    };

    private IActionResult ToReadFailureResult<T>(ApiCallResult<T> result) =>
        result.FailureKind switch
        {
            ApiFailureKind.NotFound => NotFound(),
            ApiFailureKind.Forbidden or ApiFailureKind.Unauthorized => Forbid(),
            _ => View("~/Views/Home/Error.cshtml", new ErrorViewModel())
        };

    private static void NormalizeEventFilter(EventListFilter filter)
    {
        filter.PageNumber = Math.Max(filter.PageNumber, 1);
        filter.PageSize = filter.PageSize is < 1 or > 50 ? 10 : filter.PageSize;
        filter.SortDirection = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        filter.SortColumn = filter.SortColumn is "Title" or "Location" ? filter.SortColumn : null;
    }

    private static string? NormalizeMeetingUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
