using System.Globalization;
using System.Text;
using ConCancha.Contracts.Customers;

namespace ConCancha.Application.Customers;

/// <summary>
/// Customer list as CSV for Excel in Spanish locales: <c>;</c> separator and a UTF-8 byte order mark so accents survive.
/// </summary>
public static class CustomerCsvFormatter
{
    private const char Separator = ';';
    private static readonly string[] Header = ["Cliente", "WhatsApp", "Mail", "Turnos", "Gastó", "Debe", "Faltó"];

    public static byte[] Format(IEnumerable<CustomerSummaryDto> customers)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(Separator, Header));
        foreach (var customer in customers) builder.AppendLine(FormatRow(customer));

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    private static string FormatRow(CustomerSummaryDto customer) => string.Join(Separator,
        Escape(customer.Name),
        Escape(customer.Phone),
        Escape(customer.Email),
        customer.Visits.ToString(CultureInfo.InvariantCulture),
        customer.Spent.ToString("0.##", CultureInfo.InvariantCulture),
        customer.Debt.ToString("0.##", CultureInfo.InvariantCulture),
        customer.NoShows.ToString(CultureInfo.InvariantCulture));

    /// <summary>Quotes the field, and defuses a leading formula character so a name cannot run as a spreadsheet formula.</summary>
    private static string Escape(string? field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;

        var safe = field[0] is '=' or '+' or '-' or '@' ? "'" + field : field;
        return '"' + safe.Replace("\"", "\"\"") + '"';
    }
}
