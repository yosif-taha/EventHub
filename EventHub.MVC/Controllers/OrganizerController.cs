using EventHub.Domin.Constants;
using EventHub.Domin.Enums;
using EventHub.MVC.Models;
using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Organizers;
using EventHub.MVC.Services.Api;
using EventHub.MVC.Services.Events;
using EventHub.MVC.Services.Organizers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.MVC.Controllers;

[Authorize(Roles = RoleNames.Organizer)]
[Route("Organizer")]
public sealed class OrganizerController(
    IOrganizerApiClient organizerApiClient,
    IEventApiClient eventApiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var result = await organizerApiClient.GetDashboardAsync(cancellationToken);
        return View(new OrganizerDashboardViewModel
        {
            Dashboard = result.Data ?? new(),
            ErrorMessage = result.IsSuccess ? null : result.Message
        });
    }

    [HttpGet("Events")]
    public async Task<IActionResult> Events([FromQuery] EventListFilter filter, CancellationToken cancellationToken)
    {
        NormalizeFilter(filter);

        var eventsTask = organizerApiClient.GetEventsAsync(filter, cancellationToken);
        var categoriesTask = eventApiClient.GetCategoriesAsync(cancellationToken);
        await Task.WhenAll(eventsTask, categoriesTask);

        var eventsResult = await eventsTask;
        var categoriesResult = await categoriesTask;
        return View(new OrganizerEventListViewModel
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

        var result = await organizerApiClient.CreateEventAsync(new CreateOrganizerEventRequest(
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
        var result = await organizerApiClient.GetEventAsync(id, cancellationToken);
        if (!result.IsSuccess || result.Data is null)
            return ToReadFailureResult(result);

        var model = new EventEditorViewModel
        {
            IsEdit = true,
            Title = result.Data.Title,
            Description = result.Data.Description,
            EventDate = ToUtc(result.Data.EventDate),
            Location = result.Data.Location,
            MaxAttendees = result.Data.MaxAttendees,
            Mode = result.Data.Mode,
            OnlineMeetingUrl = result.Data.OnlineMeetingUrl,
            Price = (double)result.Data.Price,
            CurrentCategoryName = result.Data.CategoryName
        };

        return View(await PopulateCategoriesAsync(model, cancellationToken));
    }

    [HttpPost("Events/{id:guid}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EventEditorViewModel model, CancellationToken cancellationToken)
    {
        model.IsEdit = true;
        if (!ModelState.IsValid)
            return View(await PopulateCategoriesAsync(model, cancellationToken));

        var result = await organizerApiClient.UpdateEventAsync(id, new UpdateOrganizerEventRequest(
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
        var result = await organizerApiClient.UpdateEventStatusAsync(id, EventStatus.Canceled, cancellationToken);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess
            ? "Event canceled successfully. Confirmed attendees will be notified."
            : result.Message;
        return RedirectToAction(nameof(Events));
    }

    [HttpGet("Events/{id:guid}/Attendees")]
    public async Task<IActionResult> Attendees(Guid id, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        var registrationsTask = organizerApiClient.GetAttendeesAsync(id, Math.Max(1, pageNumber), 10, cancellationToken);
        var eventTask = organizerApiClient.GetEventAsync(id, cancellationToken);
        await Task.WhenAll(registrationsTask, eventTask);

        var registrationsResult = await registrationsTask;
        if (!registrationsResult.IsSuccess)
            return ToReadFailureResult(registrationsResult);

        var eventResult = await eventTask;
        return View(new OrganizerAttendeesViewModel
        {
            EventId = id,
            EventTitle = eventResult.Data?.Title ?? "Event attendees",
            Attendees = registrationsResult.Data ?? new(),
            ErrorMessage = null
        });
    }

    [HttpGet("Events/{id:guid}/Statistics")]
    public async Task<IActionResult> Statistics(Guid id, CancellationToken cancellationToken)
    {
        var registrationsTask = organizerApiClient.GetAttendeesAsync(id, 1, 1, cancellationToken);
        var eventTask = organizerApiClient.GetEventAsync(id, cancellationToken);
        await Task.WhenAll(registrationsTask, eventTask);

        var registrationsResult = await registrationsTask;
        if (!registrationsResult.IsSuccess)
            return ToReadFailureResult(registrationsResult);

        var eventResult = await eventTask;
        if (!eventResult.IsSuccess || eventResult.Data is null)
            return ToReadFailureResult(eventResult);

        return View(new OrganizerEventStatisticsViewModel
        {
            Event = eventResult.Data,
            RegistrationCount = registrationsResult.Data?.TotalCount ?? 0
        });
    }

    [HttpGet("Events/{id:guid}/Announcement")]
    public async Task<IActionResult> Announcement(Guid id, CancellationToken cancellationToken)
    {
        var registrationsTask = organizerApiClient.GetAttendeesAsync(id, 1, 1, cancellationToken);
        var eventTask = organizerApiClient.GetEventAsync(id, cancellationToken);
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

        var result = await organizerApiClient.SendAnnouncementAsync(
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

    private async Task<EventEditorViewModel> PopulateCategoriesAsync(EventEditorViewModel model, CancellationToken cancellationToken)
    {
        var categories = await eventApiClient.GetCategoriesAsync(cancellationToken);
        model.Categories = categories.Data ?? [];
        model.CategoryLoadError = categories.IsSuccess ? null : categories.Message;
        return model;
    }

    private async Task<IActionResult> ReloadAnnouncementAsync(EventAnnouncementViewModel model, CancellationToken cancellationToken)
    {
        var registrationsResult = await organizerApiClient.GetAttendeesAsync(model.EventId, 1, 1, cancellationToken);
        if (!registrationsResult.IsSuccess)
            return ToReadFailureResult(registrationsResult);

        var eventResult = await organizerApiClient.GetEventAsync(model.EventId, cancellationToken);
        if (!eventResult.IsSuccess || eventResult.Data is null)
            return ToReadFailureResult(eventResult);

        model.EventTitle = eventResult.Data.Title;
        model.RegistrationCount = registrationsResult.Data?.TotalCount ?? 0;
        return View(nameof(Announcement), model);
    }

    private IActionResult ToReadFailureResult<T>(ApiCallResult<T> result) =>
        result.FailureKind switch
        {
            ApiFailureKind.NotFound => NotFound(),
            ApiFailureKind.Forbidden or ApiFailureKind.Unauthorized => Forbid(),
            _ => View("~/Views/Home/Error.cshtml", new ErrorViewModel())
        };

    private static void NormalizeFilter(EventListFilter filter)
    {
        filter.PageNumber = Math.Max(filter.PageNumber, 1);
        filter.PageSize = filter.PageSize is < 1 or > 50 ? 10 : filter.PageSize;
        filter.SortDirection = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        filter.SortColumn = filter.SortColumn is "Title" or "Location" or "EventDate" or "Status" ? filter.SortColumn : null;
    }

    private static string? NormalizeMeetingUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime ToUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
