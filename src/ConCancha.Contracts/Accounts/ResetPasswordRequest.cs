using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Accounts;

public record ResetPasswordRequest(
    [Required, EmailAddress, MaxLength(AccountLimits.EmailMaxLength)] string Email,
    [Required] string Token,
    [Required, MaxLength(AccountLimits.PasswordMaxLength)] string NewPassword);
