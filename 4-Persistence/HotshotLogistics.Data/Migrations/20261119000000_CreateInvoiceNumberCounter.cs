#pragma warning disable SA1649

// Allocates INV###### invoice numbers from a one-row counter that is advanced by a single atomic UPDATE.
// The counter starts at the highest sequential INV###### number already stored, so new numbers never repeat
// an existing one. FluentMigrator runs this migration in one transaction, so a failure leaves nothing
// behind and the migration can simply be run again. No dynamic SQL: a SQL Server SEQUENCE needs its start
// value as a literal in DDL, which would mean concatenating a value into a statement.
namespace HotshotLogistics.Data.Migrations;

[Migration(20261119000000)]
public class CreateInvoiceNumberCounter : Migration
{
    public override void Up()
    {
        Create.Table("InvoiceNumberCounters")
            .WithColumn("Name").AsString(50).PrimaryKey()
            .WithColumn("LastNumber").AsInt32().NotNullable();

        // Only sequential INV000001-style numbers count. Seeded values such as INV-12345 and INV{ticks}
        // do not participate.
        Execute.Sql(@"
            INSERT INTO InvoiceNumberCounters (Name, LastNumber)
            SELECT 'Invoice', ISNULL(MAX(CAST(SUBSTRING(InvoiceNumber, 4, 6) AS INT)), 0)
            FROM Invoices
            WHERE LEN(InvoiceNumber) = 9
              AND InvoiceNumber LIKE 'INV[0-9][0-9][0-9][0-9][0-9][0-9]'");
    }

    public override void Down()
    {
        Delete.Table("InvoiceNumberCounters");
    }
}
