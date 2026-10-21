using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Courts;

namespace ConCancha.Contracts.Courts;

public record CourtRequest(
    [Required(ErrorMessage = "Poné el nombre de la cancha."),
     MinLength(CourtLimits.NameMinLength, ErrorMessage = "Poné el nombre de la cancha."),
     MaxLength(CourtLimits.NameMaxLength)] string Name,
    Sport Sport,
    int SlotMinutes,
    int? OpeningMinuteOfDay,
    int? ClosingMinuteOfDay,
    bool IsActive = true);
