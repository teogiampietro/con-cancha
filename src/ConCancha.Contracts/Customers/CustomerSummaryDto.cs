namespace ConCancha.Contracts.Customers;

/// <param name="Phone">Null for people loaded at the front desk without a phone.</param>
/// <param name="Visits">Turns that were not cancelled and not missed.</param>
/// <param name="Spent">What they paid so far.</param>
/// <param name="Debt">What they still owe.</param>
/// <param name="NoShows">Turns they booked and did not show up to.</param>
public record CustomerSummaryDto(
    Guid? Id,
    string Name,
    string? Phone,
    string? WhatsAppPhone,
    string? Email,
    int Visits,
    decimal Spent,
    decimal Debt,
    int NoShows);
