using System.ComponentModel.DataAnnotations;
using ConCancha.Domain.Clubs;

namespace ConCancha.Contracts.Clubs;

public record CreateClubRequest(
    [Required(ErrorMessage = "Poné el nombre del club."),
     MinLength(ClubLimits.NameMinLength, ErrorMessage = "Poné el nombre del club."),
     MaxLength(ClubLimits.NameMaxLength)] string Name,
    string? Slug,
    [MaxLength(ClubLimits.PhoneMaxLength)] string? Phone);
