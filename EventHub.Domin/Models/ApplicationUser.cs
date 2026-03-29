using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Domin.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public ApplicationUser()
        {
            Id = Guid.CreateVersion7();
        }
        public string FullName { get; set; } = string.Empty;

        public List<Event> CreatedEvents { get; set; } = new List<Event>();  // Navigation property for events created by the user
        public List<Registration> Registrations { get; set; } = new List<Registration>(); // Navigation property for registrations of the user
        public List<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>(); // Navigation property for payment transactions of the user
        public List<Notification> Notifications { get; set; } = new List<Notification>(); // Navigation property for notifications of the user

    }
}
