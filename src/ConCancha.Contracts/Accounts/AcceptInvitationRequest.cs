using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Accounts;

public record AcceptInvitationRequest(
    [Required, EmailAddress, MaxLength(AccountLimits.EmailMaxLength)] string Email,
    [Required] string Token,
    [Required(ErrorMessage = "Poné una contraseña."), MaxLength(AccountLimits.PasswordMaxLength)] string Password);
