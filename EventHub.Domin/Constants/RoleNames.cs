using EventHub.Domin.Enums;

namespace EventHub.Domin.Constants
{
    public static class RoleNames
    {
        public const string Admin = nameof(UserRole.Admin);
        public const string Organizer = nameof(UserRole.Organizer);
        public const string Attendee = nameof(UserRole.Attendee);

        public const string AdminOrOrganizer = Admin + "," + Organizer;
        public const string AllUsers = Admin + "," + Organizer + "," + Attendee;
    }
}
