using System.Reflection;
using ConCancha.Application.Abstractions;
using ConCancha.Domain.Clubs;
using ConCancha.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Persistence;

public class ConCanchaDbContext(DbContextOptions<ConCanchaDbContext> options, ITenantContext? tenant = null)
    : IdentityDbContext<AppUser>(options)
{
    private const int IdentityUserIdMaxLength = 450;

    private static readonly MethodInfo ApplyClubFilterMethod = typeof(ConCanchaDbContext)
        .GetMethod(nameof(ApplyClubFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubMember> ClubMembers => Set<ClubMember>();

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
