using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Accounts;

namespace ConCancha.Contracts.Clubs;

public record InviteTeamMemberRequest(
    [Required(ErrorMessage = "Poné el mail de la persona."),
     EmailAddress(ErrorMessage = "Revisá el mail: no parece válido."),
     MaxLength(AccountLimits.EmailMaxLength)] string Email);
