using System.Net.Http.Json;
using ConCancha.Contracts.Accounts;
using ConCancha.Contracts.Platform;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Customers;
using ConCancha.Infrastructure.Persistence;
using ConCancha.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConCancha.Tests.Support;

/// <summary>Rows for the platform figures, written straight to the database, and the platform admin's sign-in.</summary>
public static class PlatformTestData
{
    public const int SlotMinutes = 90;

    public static async Task<Club> SeedClubAsync(ApiFactory api, DateTimeOffset? createdAt = null)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        var club = new Club { Name = "Club de prueba", Slug = ApiClientExtensions.NewSlug(), CreatedAt = createdAt ?? DateTimeOffset.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        return club;
    }

    public static async Task<Court> SeedCourtAsync(ApiFactory api, Club club, string name, bool isActive = true)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        var court = new Court { ClubId = club.Id, Name = name, Sport = Sport.Padel, SlotMinutes = SlotMinutes, IsActive = isActive };
        db.Courts.Add(court);
        await db.SaveChangesAsync();
        return court;
    }

    public static async Task<Customer> SeedCustomerAsync(ApiFactory api, Club club, string name, string phone)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        var customer = new Customer { ClubId = club.Id, Name = name, NormalizedPhone = phone };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    /// <summary>Seeds a turn that starts at the given instant. Pass <c>null</c> for <paramref name="customer"/> for a block.</summary>
    public static async Task SeedTurnAsync(
        ApiFactory api, Club club, Court court, DateTimeOffset startsAt, Customer? customer = null,
        BookingKind kind = BookingKind.Reservation, BookingStatus status = BookingStatus.Confirmed,
        Attendance attendance = Attendance.Pending, BookingOrigin origin = BookingOrigin.Panel)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConCanchaDbContext>();
        db.Bookings.Add(new Booking
        {
            ClubId = club.Id, CourtId = court.Id, CustomerId = customer?.Id, Kind = kind, Status = status,
            Attendance = attendance, Origin = origin, StartsAt = startsAt, EndsAt = startsAt.AddMinutes(SlotMinutes),
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Instant of a local Argentina day and minute, the way a player's turn is stored.</summary>
    public static DateTimeOffset Local(DateOnly day, int hour, int minute = 0) =>
        PlatformReporting.Zone.ToUtc(day, hour * 60 + minute);

    public static DateOnly LocalToday() => PlatformReporting.Zone.ToLocal(DateTimeOffset.UtcNow).Date;

    public static async Task<string> LoginAsPlatformAdminAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var response = await http.PostAsJsonAsync(
            "api/platform/auth/login",
            new { User = ApiFactory.PlatformAdminUser, Password = ApiFactory.PlatformAdminPassword },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionResponse>(cancellationToken))!.Token;
    }
}
