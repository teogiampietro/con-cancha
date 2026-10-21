using ConCancha.Application.Messages;
using Microsoft.AspNetCore.Identity;

namespace ConCancha.Infrastructure.Identity;

/// <summary>Identity messages in Spanish, ready to show to the user.</summary>
public sealed class SpanishIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"La contraseña tiene que tener al menos {length} caracteres." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = AccountMessages.EmailAlreadyRegistered };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateEmail), Description = AccountMessages.EmailAlreadyRegistered };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "Revisá el mail: no parece válido." };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = "El link para restablecer la contraseña venció o ya se usó. Pedí uno nuevo." };
}
