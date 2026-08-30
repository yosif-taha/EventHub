using EventHub.Domin.Enums;

namespace EventHub.MVC.Models.Organizers;

public sealed record CreateOrganizerEventRequest(
    string Title,
    string Description,
    DateTime EventDate,
    double Price,
    string Location,
    Guid CategoryId,
    int MaxAttendees,
    EventMode Mode,
    string? OnlineMeetingUrl);

public sealed record UpdateOrganizerEventRequest(
    Guid Id,
    string? Title,
    string? Description,
    DateTime? EventDate,
    string? Location,
    Guid? CategoryId,
    int? MaxAttendees,
    EventMode? Mode,
    string? OnlineMeetingUrl);

public sealed record UpdateEventStatusRequest(EventStatus Status);

public sealed record SendEventAnnouncementRequest(Guid EventId, string Subject, string Message);
