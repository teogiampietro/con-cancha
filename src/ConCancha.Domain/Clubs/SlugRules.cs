using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ConCancha.Domain.Clubs;

/// <summary>Rules for a club's public link: <c>domain/{slug}</c>.</summary>
public static partial class SlugRules
{
    public const int MinLength = 3;
    public const int MaxLength = 30;
    private const char Separator = '-';

    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>(StringComparer.Ordinal)
    {
        "panel", "api", "jugador", "c", "admin", "login", "registro", "asistente", "broadcast",
        "www", "app", "concancha", "swagger", "health", "static", "assets", "_framework", "_blazor",
    };

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex ValidFormat();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidCharacters();

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var slug = InvalidCharacters()
            .Replace(RemoveDiacritics(text).ToLowerInvariant(), Separator.ToString())
            .Trim(Separator);

        return slug.Length > MaxLength ? slug[..MaxLength].TrimEnd(Separator) : slug;
    }

    /// <summary>Returns why the slug is unusable, or <c>null</c> when valid (availability is not checked).</summary>
    public static string? Validate(string? slug)
    {
        if (string.IsNullOrEmpty(slug) || slug.Length < MinLength) return $"Usá al menos {MinLength} caracteres.";
        if (slug.Length > MaxLength) return $"Máximo {MaxLength} caracteres.";
        if (!ValidFormat().IsMatch(slug)) return "Solo letras, números y guiones, sin guiones al principio ni al final.";
        if (Reserved.Contains(slug)) return "Ese link está reservado.";
        return null;
    }

    private static string RemoveDiacritics(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }
        return builder.ToString();
    }
}
