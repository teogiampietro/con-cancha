using System.Text;
using ConCancha.Infrastructure.Identity;

namespace ConCancha.Api.Commands;

/// <summary>
/// Prints the hash for the platform admin password, to paste into Platform__AdminPasswordHash. Run it on your own
/// machine: the password is read without echo and the hash goes only to this terminal, never to a log or a file.
/// </summary>
public static class HashPasswordCommand
{
    public const string Name = "hash-password";
    private const int MinimumLength = 16;

    public static bool IsRequested(string[] args) => args.Contains(Name);

    public static void Run()
    {
        Console.Write("Contraseña nueva (no se muestra): ");
        var password = ReadSecret();
        if (password.Length < MinimumLength)
        {
            Console.Error.WriteLine($"Usá al menos {MinimumLength} caracteres.");
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine("Platform__AdminPasswordHash=" + PlatformPasswords.Hash(password));
    }

    private static string ReadSecret()
    {
        var secret = new StringBuilder();
        for (var key = Console.ReadKey(intercept: true); key.Key != ConsoleKey.Enter; key = Console.ReadKey(intercept: true))
        {
            if (key.Key != ConsoleKey.Backspace) secret.Append(key.KeyChar);
            else if (secret.Length > 0) secret.Length--;
        }

        Console.WriteLine();
        return secret.ToString();
    }
}
