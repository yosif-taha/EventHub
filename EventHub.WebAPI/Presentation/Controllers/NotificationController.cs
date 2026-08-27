using EventHub.Application.Common.Extensions;
using EventHub.Application.Features.Notifications.SendEventAnnouncement;
using EventHub.Domin.Constants;
using EventHub.WebAPI.Presentation.ViewModels.Notifications;
using EventHub.WebAPI.Presentation.ViewModels.Respponse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.WebAPI.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class NotificationController(IMediator _mediator) : ControllerBase
    {
        [HttpPost("/api/notifications")]
        [Authorize(Roles = RoleNames.AdminOrOrganizer)]
        public async Task<ResponseViewModel> SendEventAnnouncement(
            [FromBody] SendEventAnnouncementRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(new SendEventAnnouncementCommand(request.EventId, request.Subject, request.Message), ct);
            if (!result.IsSuccess)
                return new FailedResponseViewModel(result.ErrorCode, result.Message ?? result.ErrorCode.GetDescription());

            return new SuccessResponseViewModel($"Announcement queued for {result.Data} attendee(s).");
        }
    }
}
