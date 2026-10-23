using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Accounts;

public record VerifyEmailRequest(
    [Required, EmailAddress, MaxLength(AccountLimits.EmailMaxLength)] string Email,
    [Required(ErrorMessage = "Poné el código que te mandamos."),
     MaxLength(AccountLimits.VerificationCodeMaxLength)] string Code);
