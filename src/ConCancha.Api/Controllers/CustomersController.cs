using ConCancha.Api.Auth;
using ConCancha.Application.Abstractions;
using ConCancha.Application.Customers;
using ConCancha.Contracts.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConCancha.Api.Controllers;

[Authorize(Roles = ClubRoles.Any)]
public class CustomersController(ICustomerDirectoryService customers) : ApiControllerBase
{
    private const string CsvContentType = "text/csv; charset=utf-8";
    private const string CsvFileName = "clientes.csv";

    [HttpGet("api/customers")]
    public async Task<ActionResult<IReadOnlyList<CustomerSummaryDto>>> List(
        [FromQuery] string? search, [FromQuery] bool onlyDebtors, [FromQuery] bool onlyNoShows, CancellationToken cancellationToken) =>
        Ok(await customers.ListAsync(search, onlyDebtors, onlyNoShows, cancellationToken));

    /// <summary>The same list, as a CSV file that opens in Excel.</summary>
    [HttpGet("api/customers/export")]
    public async Task<IActionResult> Export(
        [FromQuery] string? search, [FromQuery] bool onlyDebtors, [FromQuery] bool onlyNoShows, CancellationToken cancellationToken)
    {
        var list = await customers.ListAsync(search, onlyDebtors, onlyNoShows, cancellationToken);
        return File(CustomerCsvFormatter.Format(list), CsvContentType, CsvFileName);
    }
}
