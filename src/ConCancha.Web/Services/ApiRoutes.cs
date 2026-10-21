namespace ConCancha.Web.Services;

public static class ApiRoutes
{
    public const string Register = "api/auth/register";
    public const string Login = "api/auth/login";
    public const string ForgotPassword = "api/auth/forgot-password";
    public const string ResetPassword = "api/auth/reset-password";
    public const string Clubs = "api/clubs";
    public const string SlugAvailability = "api/clubs/slug-availability";
    public const string CurrentClub = "api/club";
    public const string CurrentClubTeam = "api/club/team";
}
