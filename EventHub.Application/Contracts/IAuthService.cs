using EventHub.Application.Common.Dtos.Auth;
using EventHub.Domin.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Application.Contracts
{
    public interface IAuthService
    {
        Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken ct = default);
        Task<bool> RegisterAsync(string email, string password, string fullName, UserRole role = UserRole.Attend, CancellationToken ct = default);
        Task<AuthResponse?> GenerateNewTokensAsync(string token, string refreshToken, CancellationToken ct = default);
        Task<bool> ConfirmEmailAsync(string userId, string urlCode);
        Task<bool> ResendConfirmationEmailAsync(string email);
        Task<bool> SendResetPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string code, string newPassword);
    }
}
