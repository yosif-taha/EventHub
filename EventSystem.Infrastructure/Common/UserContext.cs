using EventHub.Application.Contracts;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EventHub.Infrastructure.Common
{
    public class UserContext(IHttpContextAccessor _httpContextAccessor) : IUserContext
    {
        public Guid UserId =>
            Guid.TryParse( _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : Guid.Empty;
        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
