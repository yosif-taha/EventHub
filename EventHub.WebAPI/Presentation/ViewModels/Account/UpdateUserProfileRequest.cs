namespace EventHub.WebAPI.Presentation.ViewModels.Account
{
    public class UpdateUserProfileRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

    }
}
