using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Clubs;

namespace ConCancha.Contracts.Clubs;

public record UpdateClubProfileRequest(
    [Required(ErrorMessage = "Poné el nombre del club."),
     MinLength(ClubLimits.NameMinLength, ErrorMessage = "Poné el nombre del club."),
     MaxLength(ClubLimits.NameMaxLength, ErrorMessage = "El nombre es demasiado largo.")] string Name,
    [MaxLength(ClubLimits.PhoneMaxLength, ErrorMessage = "El teléfono es demasiado largo.")] string? Phone);
