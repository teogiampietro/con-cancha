using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Accounts;

public record ForgotPasswordRequest([Required, EmailAddress, MaxLength(AccountLimits.EmailMaxLength)] string Email);
