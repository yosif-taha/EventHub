using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Common.Dtos.Category
{
    public record CategoryDto(
        Guid Id,
        string Name,
        int EventsCount
    );
}
