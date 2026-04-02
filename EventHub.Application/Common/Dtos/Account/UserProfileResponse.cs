using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Common.Dtos.Account
{
    public record UserProfileResponse(
        string Email,
        string UserNAme,
        string FullName
        );
    
}
