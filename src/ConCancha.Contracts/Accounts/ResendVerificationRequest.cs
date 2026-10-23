using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Accounts;

public record ResendVerificationRequest(
    [Required, EmailAddress, MaxLength(AccountLimits.EmailMaxLength)] string Email);
