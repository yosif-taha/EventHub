
namespace EventHub.WebAPI.Presentation.ViewModels.Auth
{
    public class AuthResponseViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public DateTime RefreshTokenExpiration { get; set; }
    }
}
