using System.Reflection;
using ConCancha.Application.Abstractions;
using ConCancha.Domain.Accounts;
using ConCancha.Domain.Bookings;
using ConCancha.Domain.Cash;
using ConCancha.Domain.Clubs;
using ConCancha.Domain.Courts;
using ConCancha.Domain.Customers;
using ConCancha.Domain.Pricing;
using ConCancha.Domain.Setup;
using ConCancha.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Persistence;

public class ConCanchaDbContext(DbContextOptions<ConCanchaDbContext> options, ITenantContext? tenant = null)
    : IdentityDbContext<AppUser>(options)
{
    private const int IdentityUserIdMaxLength = 450;
    private const int ManagementTokenHashLength = 64;

    private static readonly MethodInfo ApplyClubFilterMethod = typeof(ConCanchaDbContext)
        .GetMethod(nameof(ApplyClubFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
    public DbSet<ClubInvitation> ClubInvitations => Set<ClubInvitation>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<ClubSetup> ClubSetups => Set<ClubSetup>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CashClosure> CashClosures => Set<CashClosure>();

    /// <summary>
    /// Club that queries filter by. With no club set it is <c>null</c> and the filters return nothing:
    /// ask for the club explicitly or use <c>IgnoreQueryFilters()</c> in a service that justifies it.
    /// </summary>
    public Guid? CurrentClubId => tenant?.ClubId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureClub(modelBuilder);
        ConfigureClubMember(modelBuilder);
        ConfigureClubInvitation(modelBuilder);
        ConfigureCourt(modelBuilder);
        ConfigurePriceRule(modelBuilder);
        ConfigureBooking(modelBuilder);
        ConfigureClubSetup(modelBuilder);
        ConfigureCustomer(modelBuilder);
        ConfigureCashClosure(modelBuilder);
        ApplyClubFilters(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardClubWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardClubWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private static void ConfigureClub(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Club>(entity =>
        {
            entity.Property(club => club.Name).HasMaxLength(ClubLimits.NameMaxLength).IsRequired();
            entity.Property(club => club.Slug).HasMaxLength(SlugRules.MaxLength).IsRequired();
            entity.Property(club => club.Phone).HasMaxLength(ClubLimits.PhoneMaxLength);
            entity.Property(club => club.CountryCode).HasMaxLength(ClubLimits.CountryCodeLength).IsRequired();
            entity.Property(club => club.TimeZoneId).HasMaxLength(ClubLimits.TimeZoneMaxLength).IsRequired();
            entity.Property(club => club.LogoPath).HasMaxLength(ClubLimits.LogoPathMaxLength);
            entity.Property(club => club.PrimaryColor).HasMaxLength(ClubLimits.ColorLength).IsRequired().HasDefaultValue(ClubBranding.DefaultPrimaryColor);
            entity.Property(club => club.SecondaryColor).HasMaxLength(ClubLimits.ColorLength).IsRequired().HasDefaultValue(ClubBranding.DefaultSecondaryColor);
            entity.Property(club => club.EnabledModules).HasConversion<int>().HasDefaultValue(ClubModules.All);
            entity.HasIndex(club => club.Slug).IsUnique();
        });

    private static void ConfigureClubMember(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<ClubMember>(entity =>
        {
            entity.Property(member => member.UserId).HasMaxLength(IdentityUserIdMaxLength).IsRequired();
            entity.HasOne(member => member.Club).WithMany().HasForeignKey(member => member.ClubId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AppUser>().WithMany().HasForeignKey(member => member.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(member => new { member.ClubId, member.UserId }).IsUnique();
            entity.HasIndex(member => member.UserId);
        });

    private static void ConfigureClubInvitation(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<ClubInvitation>(entity =>
        {
            entity.Property(invitation => invitation.Email).HasMaxLength(AccountLimits.EmailMaxLength).IsRequired();
            entity.Property(invitation => invitation.TokenHash).HasMaxLength(ManagementTokenHashLength).IsRequired();
            entity.Property(invitation => invitation.Role).HasConversion<int>();
            entity.Property(invitation => invitation.Status).HasConversion<int>();
            entity.HasOne<Club>().WithMany().HasForeignKey(invitation => invitation.ClubId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(invitation => invitation.TokenHash);
            entity.HasIndex(invitation => new { invitation.ClubId, invitation.Email });
        });

    private static void ConfigureCourt(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Court>(entity =>
        {
            entity.Property(court => court.Name).HasMaxLength(CourtLimits.NameMaxLength).IsRequired();
            entity.Property(court => court.Sport).HasConversion<int>();
            entity.HasOne(court => court.Club).WithMany().HasForeignKey(court => court.ClubId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(court => new { court.ClubId, court.Name }).IsUnique();
        });

    private static void ConfigurePriceRule(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<PriceRule>(entity =>
        {
            entity.Property(rule => rule.Price).HasPrecision(PriceLimits.MoneyPrecision, PriceLimits.MoneyScale);
            entity.Property(rule => rule.Days).HasConversion<int?>();
            entity.HasOne(rule => rule.Court).WithMany().HasForeignKey(rule => rule.CourtId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(rule => rule.CourtId);
        });

    /// <summary>
    /// The no-overlap guarantee is a PostgreSQL exclusion constraint created in the migration
    /// (EF cannot express it with a partial predicate); see <c>AddCourtsPricingAndBookings</c>.
    /// </summary>
    private static void ConfigureBooking(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(booking => booking.Price).HasPrecision(PriceLimits.MoneyPrecision, PriceLimits.MoneyScale);
            entity.Property(booking => booking.CustomerName).HasMaxLength(BookingLimits.CustomerNameMaxLength);
            entity.Property(booking => booking.CustomerPhone).HasMaxLength(BookingLimits.PhoneMaxLength);
            entity.Property(booking => booking.Reason).HasMaxLength(BookingLimits.ReasonMaxLength);
            entity.Property(booking => booking.Kind).HasConversion<int>();
            entity.Property(booking => booking.Status).HasConversion<int>();
            entity.Property(booking => booking.PaymentStatus).HasConversion<int>();
            entity.Property(booking => booking.PaymentMethod).HasConversion<int?>();
            entity.Property(booking => booking.Attendance).HasConversion<int>();
            entity.Property(booking => booking.Origin).HasConversion<int>();
            entity.Property(booking => booking.ManagementTokenHash).HasMaxLength(ManagementTokenHashLength);
            entity.Property(booking => booking.VisitorTokenHash).HasMaxLength(ManagementTokenHashLength);
            entity.HasIndex(booking => booking.VisitorTokenHash);
            entity.HasOne<Customer>().WithMany().HasForeignKey(booking => booking.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(booking => booking.ManagementTokenHash);
            entity.HasIndex(booking => booking.CustomerId);
            entity.HasOne(booking => booking.Court).WithMany().HasForeignKey(booking => booking.CourtId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(booking => new { booking.ClubId, booking.StartsAt });
        });

    private static void ConfigureCustomer(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(customer => customer.Name).HasMaxLength(BookingLimits.CustomerNameMaxLength).IsRequired();
            entity.Property(customer => customer.NormalizedPhone).HasMaxLength(CustomerLimits.NormalizedPhoneMaxLength).IsRequired();
            entity.Property(customer => customer.Email).HasMaxLength(CustomerLimits.EmailMaxLength);
            entity.HasOne<Club>().WithMany().HasForeignKey(customer => customer.ClubId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(customer => new { customer.ClubId, customer.NormalizedPhone }).IsUnique();
        });

    private static void ConfigureCashClosure(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<CashClosure>(entity =>
        {
            entity.Property(closure => closure.Total).HasPrecision(PriceLimits.MoneyPrecision, PriceLimits.MoneyScale);
            entity.HasOne<Club>().WithMany().HasForeignKey(closure => closure.ClubId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(closure => new { closure.ClubId, closure.Date }).IsUnique();
        });

    private static void ConfigureClubSetup(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<ClubSetup>(entity =>
        {
            entity.Property(setup => setup.DraftJson).HasMaxLength(SetupLimits.DraftMaxLength);
            entity.HasOne<Club>().WithMany().HasForeignKey(setup => setup.ClubId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(setup => setup.ClubId).IsUnique();
        });

    private void ApplyClubFilters(ModelBuilder modelBuilder)
    {
        var clubOwnedTypes = modelBuilder.Model.GetEntityTypes()
            .Where(entityType => typeof(IBelongsToClub).IsAssignableFrom(entityType.ClrType));

        foreach (var entityType in clubOwnedTypes)
            ApplyClubFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
    }

    private void ApplyClubFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IBelongsToClub =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(entity => entity.ClubId == CurrentClubId);

    /// <summary>With a club set, writing to another club is forbidden: a missing ClubId is filled in, a different one rejected.</summary>
    private void GuardClubWrites()
    {
        if (CurrentClubId is not { } currentClubId) return;

        var pendingWrites = ChangeTracker.Entries<IBelongsToClub>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in pendingWrites)
            AssignOrRejectClub(entry.Entity, currentClubId);
    }

    private static void AssignOrRejectClub(IBelongsToClub entity, Guid currentClubId)
    {
        if (entity.ClubId == Guid.Empty)
            entity.ClubId = currentClubId;
        else if (entity.ClubId != currentClubId)
            throw new InvalidOperationException("Cannot write to a club other than the request's club.");
    }
}
