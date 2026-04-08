using EventHub.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Features.Events.Delete_Event
{
    public  record DeleteEventCommand(Guid Id) : IRequest<RequestResult<Unit>>;

}
