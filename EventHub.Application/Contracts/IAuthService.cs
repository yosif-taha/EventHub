using EventHub.Application.Common.Dtos.Auth;
using EventHub.Application.Common.Responses;
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
        Task<RequestResult<AuthResponse?>> LoginAsync(string email, string password, CancellationToken ct = default);
        Task<RequestResult<Guid>> RegisterAsync(string email, string password, string fullName, UserRole role = UserRole.Attend, CancellationToken ct = default);
        Task<RequestResult<AuthResponse?>> GenerateNewTokensAsync(string token, string refreshToken, CancellationToken ct = default);
        Task<RequestResult<bool>> ConfirmEmailAsync(string userId, string urlCode, CancellationToken ct = default);
        Task<RequestResult<bool>> ResendConfirmationEmailAsync(string email, CancellationToken ct = default);
        Task<RequestResult<bool>> SendResetPasswordAsync(string email , CancellationToken ct = default);
        Task<RequestResult<bool>> ResetPasswordAsync(string email, string code, string newPassword , CancellationToken ct = default);
    }
}
