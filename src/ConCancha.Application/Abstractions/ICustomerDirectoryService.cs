using ConCancha.Contracts.Customers;

namespace ConCancha.Application.Abstractions;

public interface ICustomerDirectoryService
{
    Task<IReadOnlyList<CustomerSummaryDto>> ListAsync(
        string? search, bool onlyDebtors, bool onlyNoShows, CancellationToken cancellationToken);
}
