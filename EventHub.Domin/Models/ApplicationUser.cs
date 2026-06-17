using EventHub.Domin.Enums;
using Microsoft.AspNetCore.Identity;

namespace EventHub.Domin.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public ApplicationUser()
        {
            Id = Guid.CreateVersion7();
        }
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Attend; // Default Role
        public List<Event> CreatedEvents { get; set; } = [];  // Navigation property for events created by the user
        public List<Registration> Registrations { get; set; } = [];// Navigation property for registrations of the user
        public List<PaymentTransaction> PaymentTransactions { get; set; } = []; // Navigation property for payment transactions of the user
        public List<Notification> Notifications { get; set; } = []; // Navigation property for notifications of the user
        public List<RefreshTokens> RefreshTokens { get; set; } = []; // Navigation property for refresh tokens of the user

    }
}
