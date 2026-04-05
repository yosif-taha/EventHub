namespace EventHub.WebAPI.Presentation.ViewModels.Auth
{
    public class RefreshTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

    }
}
