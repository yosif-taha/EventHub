using EventHub.Domin.Enums;

namespace EventHub.WebAPI.Presentation.ViewModels.Auth
{
    public class RegisterRequestViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Attend;
    }
}
