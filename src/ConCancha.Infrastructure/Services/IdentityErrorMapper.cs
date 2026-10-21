using System.Text.Json;
using ConCancha.Application.Results;
using ConCancha.Contracts.Accounts;
using Microsoft.AspNetCore.Identity;

namespace ConCancha.Infrastructure.Services;

public static class IdentityErrorMapper
{
    public static ServiceError ToValidationError(IdentityResult result, string defaultField)
    {
        var fieldErrors = result.Errors
            .GroupBy(error => ResolveField(error, defaultField))
            .ToDictionary(
                group => JsonNamingPolicy.CamelCase.ConvertName(group.Key),
                group => group.Select(error => error.Description).ToArray());

        return new ServiceError(ErrorKind.Validation, result.Errors.First().Description, fieldErrors);
    }

    public static bool IsDuplicateEmail(IdentityResult result) =>
        result.Errors.Any(error =>
            error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName));

    private static string ResolveField(IdentityError error, string defaultField) =>
        error.Code == nameof(IdentityErrorDescriber.InvalidEmail) ? nameof(RegisterRequest.Email) : defaultField;
}
