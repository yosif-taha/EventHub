using EventHub.MVC.Models.Events;
using EventHub.MVC.Models.Registrations;
using EventHub.MVC.Models;
using EventHub.MVC.Services.Events;
using EventHub.MVC.Services.Registrations;
using EventHub.Domin.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.MVC.Controllers;

[AllowAnonymous]
[Route("Events")]
public sealed class EventsController(
    IEventApiClient eventApiClient,
    IRegistrationApiClient registrationApiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] EventListFilter filter, CancellationToken cancellationToken)
    {
        filter.PageNumber = Math.Max(filter.PageNumber, 1);
        filter.PageSize = filter.PageSize is < 1 or > 50 ? 10 : filter.PageSize;
        filter.SortDirection = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
        filter.SortColumn = filter.SortColumn is "Title" or "Location" ? filter.SortColumn : null;

        var eventsTask = eventApiClient.GetEventsAsync(filter, cancellationToken);
        var categoriesTask = eventApiClient.GetCategoriesAsync(cancellationToken);
        await Task.WhenAll(eventsTask, categoriesTask);

        var eventsResult = await eventsTask;
        var categoriesResult = await categoriesTask;
        return View(new EventListViewModel
        {
            Filter = filter,
            Events = eventsResult.Data ?? new(),
            Categories = categoriesResult.Data ?? [],
            ErrorMessage = eventsResult.IsSuccess ? null : eventsResult.Message
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var eventTask = eventApiClient.GetEventAsync(id, cancellationToken);
        var availabilityTask = eventApiClient.GetAvailabilityAsync(id, cancellationToken);
        await Task.WhenAll(eventTask, availabilityTask);

        var eventResult = await eventTask;
        if (!eventResult.IsSuccess || eventResult.Data is null)
            return eventResult.FailureKind == Services.Api.ApiFailureKind.NotFound
                ? NotFound()
                : View("~/Views/Home/Error.cshtml", new ErrorViewModel());

        var availabilityResult = await availabilityTask;
        EventMeetingLinkDto? meetingLink = null;
        if (User.IsInRole(RoleNames.Attendee))
        {
            var meetingLinkResult = await registrationApiClient.GetMeetingLinkAsync(id, cancellationToken);
            meetingLink = meetingLinkResult.IsSuccess ? meetingLinkResult.Data : null;
        }

        return View(new EventDetailsViewModel
        {
            Event = eventResult.Data,
            Availability = availabilityResult.Data ?? new EventAvailabilityDto
            {
                IsAvailable = false,
                RemainingSlots = eventResult.Data.RemainingSlots,
                IsCancelled = eventResult.Data.Status.Equals("Canceled", StringComparison.OrdinalIgnoreCase)
            },
            AttendeeMeetingLink = meetingLink
        });
    }
}
