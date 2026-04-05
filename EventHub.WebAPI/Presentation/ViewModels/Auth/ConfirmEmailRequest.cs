namespace EventHub.WebAPI.Presentation.ViewModels.Auth
{
    public class ConfirmEmailRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
