using EventHub.Domin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Contracts
{
    public interface IJwtProvider
    {
       Task<(string token, int expiresIn)> GenerateTokenAsync(ApplicationUser user);
        string? ValidateToken(string token);
    }
}
