using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Clubs;

namespace ConCancha.Contracts.Clubs;

public record UpdateClubColorsRequest(
    [Required(ErrorMessage = "Elegí el color principal."),
     MaxLength(ClubLimits.ColorLength, ErrorMessage = "Elegí un color válido, por ejemplo #B8341E.")] string PrimaryColor,
    [Required(ErrorMessage = "Elegí el color secundario."),
     MaxLength(ClubLimits.ColorLength, ErrorMessage = "Elegí un color válido, por ejemplo #B8341E.")] string SecondaryColor);
