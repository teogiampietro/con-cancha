namespace ConCancha.Contracts.Platform;

/// <param name="Total">Customers matching the search, across all pages.</param>
public record PlatformCustomersPageDto(IReadOnlyList<PlatformCustomerDto> Items, int Total, int Page, int PageSize);
