using System.Text;
using ConCancha.Application.Customers;
using ConCancha.Contracts.Customers;

namespace ConCancha.Tests.Domain;

public class CustomerCsvFormatterTests
{
    private static string Format(params CustomerSummaryDto[] customers) =>
        Encoding.UTF8.GetString(CustomerCsvFormatter.Format(customers));

    [Fact]
    public void Format_StartsWithByteOrderMark_AndHeader()
    {
        var bytes = CustomerCsvFormatter.Format([]);

        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Contains("Cliente;WhatsApp;Mail;Turnos;Gastó;Debe;Faltó", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Format_WritesOneRowPerCustomer()
    {
        var csv = Format(new CustomerSummaryDto(null, "Ana", "11 5555 1234", null, null, 2, 40000m, 20000m, 1));

        Assert.Contains("\"Ana\";\"11 5555 1234\";;2;40000;20000;1", csv);
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://malo\")")]
    [InlineData("+54 11")]
    [InlineData("@SUM(A1)")]
    [InlineData("-2+3")]
    public void Format_NamesStartingWithAFormulaCharacter_AreNotExecutable(string name)
    {
        var csv = Format(new CustomerSummaryDto(null, name, null, null, null, 0, 0, 0, 0));

        Assert.Contains("\"'" + name.Replace("\"", "\"\""), csv);
    }

    [Fact]
    public void Format_QuotesAndSeparatorsInsideNames_AreEscaped()
    {
        var csv = Format(new CustomerSummaryDto(null, "Ana; \"la\" Gómez", null, null, null, 0, 0, 0, 0));

        Assert.Contains("\"Ana; \"\"la\"\" Gómez\"", csv);
    }
}
