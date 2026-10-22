namespace ConCancha.Web.Services;

public static class ApiRoutes
{
    public const string Register = "api/auth/register";
    public const string Login = "api/auth/login";
    public const string ForgotPassword = "api/auth/forgot-password";
    public const string ResetPassword = "api/auth/reset-password";
    public const string Clubs = "api/clubs";
    public const string SlugAvailability = "api/clubs/slug-availability";
    public const string PublicClubs = "api/public/clubs";
    public const string CurrentClub = "api/club";
    public const string CurrentClubTeam = "api/club/team";
    public const string ClubHours = "api/club/hours";
    public const string ClubProfile = "api/club/profile";
    public const string ClubCancellation = "api/club/cancellation";
    public const string ChangePassword = "api/auth/change-password";
    public const string Setup = "api/club/setup";
    public const string SetupComplete = "api/club/setup/complete";
    public const string Courts = "api/courts";
    public const string PriceRules = "api/price-rules";
    public const string Agenda = "api/agenda";
    public const string Reservations = "api/reservations";
    public const string Customers = "api/customers";
    public const string CustomersExport = "api/customers/export";
    public const string Cash = "api/cash";
    public const string Reminders = "api/reminders";
    public const string Bookings = "api/bookings";
    public const string Blocks = "api/bookings/blocks";
}
