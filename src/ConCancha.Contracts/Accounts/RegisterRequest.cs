using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Accounts;

public record RegisterRequest(
    [Required(ErrorMessage = "Poné tu mail."),
     EmailAddress(ErrorMessage = "Revisá el mail: no parece válido."),
     MaxLength(AccountLimits.EmailMaxLength)] string Email,
    [Required(ErrorMessage = "Poné una contraseña."),
     MaxLength(AccountLimits.PasswordMaxLength)] string Password);
