using ConCancha.Domain.Customers;
using ConCancha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConCancha.Infrastructure.Services;

/// <summary>Finds the club's customer for a WhatsApp, or adds one. The new customer is saved together with the booking that needs it.</summary>
public sealed class CustomerResolver(ConCanchaDbContext db)
{
    /// <returns>The customer's id, or <c>null</c> when the phone is not a number that identifies a person.</returns>
    public async Task<Guid?> FindOrCreateAsync(string name, string? phone, string? email, CancellationToken cancellationToken)
    {
        if (PhoneNormalizer.Normalize(phone) is not { } normalizedPhone) return null;

        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.NormalizedPhone == normalizedPhone, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            customer = new Customer { Name = name, NormalizedPhone = normalizedPhone, Email = email };
            db.Customers.Add(customer);
        }
        else if (customer.Email is null && email is not null)
        {
            customer.Email = email;
        }

        return customer.Id;
    }
}
