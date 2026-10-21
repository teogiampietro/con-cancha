using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure;

public class ConCanchaDbContext(DbContextOptions<ConCanchaDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConCanchaDbContext).Assembly);
    }
}
