using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Accounts;

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Poné tu contraseña actual."),
     MaxLength(AccountLimits.PasswordMaxLength)] string CurrentPassword,
    [Required(ErrorMessage = "Poné la contraseña nueva."),
     MaxLength(AccountLimits.PasswordMaxLength)] string NewPassword);
