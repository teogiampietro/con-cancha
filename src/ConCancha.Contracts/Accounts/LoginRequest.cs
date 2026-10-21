using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Accounts;

public record LoginRequest(
    [Required, MaxLength(AccountLimits.EmailMaxLength)] string Email,
    [Required, MaxLength(AccountLimits.PasswordMaxLength)] string Password);
