using ConCancha.Application.Abstractions;
using ConCancha.Contracts.Customers;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Customers;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

public sealed class CustomerDirectoryService(ConCanchaDbContext db) : ICustomerDirectoryService
{
    public async Task<IReadOnlyList<CustomerSummaryDto>> ListAsync(
        string? search, bool onlyDebtors, bool onlyNoShows, CancellationToken cancellationToken)
    {
        var bookings = await db.Bookings.AsNoTracking()
            .Where(booking => booking.Kind == BookingKind.Reservation).ToListAsync(cancellationToken).ConfigureAwait(false);
        var customers = await db.Customers.AsNoTracking().ToDictionaryAsync(c => c.Id, cancellationToken).ConfigureAwait(false);

        return bookings
            .GroupBy(booking => booking.CustomerId?.ToString() ?? (booking.CustomerName ?? string.Empty).Trim().ToLowerInvariant())
            .Select(group => Summarize(group.ToList(), customers))
            .Where(summary => Matches(summary, search, onlyDebtors, onlyNoShows))
            .OrderBy(summary => summary.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static CustomerSummaryDto Summarize(IReadOnlyList<Booking> bookings, IReadOnlyDictionary<Guid, Customer> customers)
    {
        var first = bookings[0];
        var customer = first.CustomerId is { } id ? customers.GetValueOrDefault(id) : null;
        var counted = bookings.Where(booking => booking.Status == BookingStatus.Confirmed).ToList();

        return new CustomerSummaryDto(
            customer?.Id,
            customer?.Name ?? first.CustomerName ?? string.Empty,
            bookings.Select(booking => booking.CustomerPhone).FirstOrDefault(phone => !string.IsNullOrEmpty(phone)),
            customer?.NormalizedPhone,
            customer?.Email,
            counted.Count(booking => booking.Attendance != Attendance.NoShow),
            counted.Sum(BookingAmounts.Collected),
            counted.Sum(BookingAmounts.ToCollect),
            counted.Count(booking => booking.Attendance == Attendance.NoShow));
    }

    private static bool Matches(CustomerSummaryDto summary, string? search, bool onlyDebtors, bool onlyNoShows)
    {
        if (onlyDebtors && summary.Debt <= 0) return false;
        if (onlyNoShows && summary.NoShows == 0) return false;
        if (string.IsNullOrWhiteSpace(search)) return true;

        var text = search.Trim();
        var digits = new string(text.Where(char.IsDigit).ToArray());
        return summary.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase)
            || (summary.Phone?.Contains(text, StringComparison.Ordinal) ?? false)
            || (digits.Length > 0 && (summary.WhatsAppPhone?.Contains(digits, StringComparison.Ordinal) ?? false))
            || (PhoneNormalizer.Normalize(text) is { } exact && summary.WhatsAppPhone == exact);
    }
}
