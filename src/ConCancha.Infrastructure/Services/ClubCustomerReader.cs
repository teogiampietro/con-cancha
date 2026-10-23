using System.Linq.Expressions;
using ConCancha.Contracts.Platform;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Customers;
using ConCancha.Domain.Platform;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

/// <summary>
/// One club's customers, searched, ordered and paged in the database: the page never loads turns into memory.
/// Reads across clubs, hence <c>IgnoreQueryFilters</c>.
/// </summary>
public sealed class ClubCustomerReader(ConCanchaDbContext db)
{
    private const string LikeEscape = "\\";

    /// <returns>The page, or <c>null</c> when the club does not exist.</returns>
    public async Task<PlatformCustomersPageDto?> ReadPageAsync(
        Guid clubId, UsagePeriod period, string? search, int page, CancellationToken cancellationToken)
    {
        if (!await db.Clubs.IgnoreQueryFilters().AnyAsync(club => club.Id == clubId, cancellationToken).ConfigureAwait(false))
            return null;

        var matching = Matching(db.Customers.IgnoreQueryFilters().AsNoTracking().Where(customer => customer.ClubId == clubId), search);
        var total = await matching.CountAsync(cancellationToken).ConfigureAwait(false);
        var rows = await OrderedPage(matching, period, clubId, page).ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PlatformCustomersPageDto(
            rows.Select(row => ToDto(row.Customer, row.InPeriod, row.LastTurn)).ToList(),
            total,
            page,
            PlatformCustomerPaging.PageSize);
    }

    /// <summary>Latest turn first, customers without turns last, then by name.</summary>
    private IQueryable<CustomerRow> OrderedPage(IQueryable<Customer> customers, UsagePeriod period, Guid clubId, int page)
    {
        var (from, to) = PlatformReporting.UtcRange(period.From, period.ToExclusive);
        var turns = Turns(clubId);

        return customers
            .OrderByDescending(LastTurnForOrder(turns))
            .ThenBy(customer => customer.Name)
            .Skip((page - 1) * PlatformCustomerPaging.PageSize).Take(PlatformCustomerPaging.PageSize)
            .Select(customer => new CustomerRow(
                customer,
                turns.Count(booking => booking.CustomerId == customer.Id && booking.StartsAt >= from && booking.StartsAt < to),
                turns.Where(booking => booking.CustomerId == customer.Id).Max(booking => (DateTimeOffset?)booking.StartsAt)));
    }

    /// <remarks>Customers without turns sort as the oldest possible date, so they fall last in a descending order.</remarks>
    private static Expression<Func<Customer, DateTimeOffset>> LastTurnForOrder(IQueryable<Booking> turns) =>
        customer => turns.Where(booking => booking.CustomerId == customer.Id)
            .Max(booking => (DateTimeOffset?)booking.StartsAt) ?? DateTimeOffset.MinValue;

    private IQueryable<Booking> Turns(Guid clubId) =>
        db.Bookings.IgnoreQueryFilters().AsNoTracking().WhereCountedTurn()
            .Where(booking => booking.ClubId == clubId && booking.CustomerId != null);

    private static IQueryable<Customer> Matching(IQueryable<Customer> customers, string? search)
    {
        var text = search?.Trim() ?? string.Empty;
        if (text.Length == 0) return customers;

        var digits = new string(text.Where(char.IsDigit).ToArray());
        var namePattern = "%" + EscapeLike(text) + "%";
        return customers.Where(customer => EF.Functions.ILike(customer.Name, namePattern, LikeEscape)
            || (digits.Length > 0 && customer.NormalizedPhone.Contains(digits)));
    }

    private static string EscapeLike(string text) =>
        text.Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_");

    private static PlatformCustomerDto ToDto(Customer customer, int inPeriod, DateTimeOffset? lastTurn) => new(
        customer.Id,
        customer.Name,
        customer.NormalizedPhone,
        customer.Email,
        inPeriod,
        lastTurn is { } last ? PlatformReporting.Zone.ToLocal(last).Date : null,
        PlatformReporting.Zone.ToLocal(customer.CreatedAt).Date);

    private sealed record CustomerRow(Customer Customer, int InPeriod, DateTimeOffset? LastTurn);
}
