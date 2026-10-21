using Microsoft.AspNetCore.Identity;

namespace ConCancha.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
