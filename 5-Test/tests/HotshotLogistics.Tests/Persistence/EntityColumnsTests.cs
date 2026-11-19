// <copyright file="EntityColumnsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace HotshotLogistics.Tests.Persistence
{
    /// <summary>
    ///     Tests that every named column list the repositories select matches the migrated schema.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class EntityColumnsTests
    {
        /// <summary>
        ///     Tests that selecting each column list from its table succeeds, so no column name is misspelled or dropped.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task ColumnLists_SelectFromTheMigratedSchema()
        {
            (string Columns, string Source)[] selections =
            [
                (EntityColumns.Customer, "Customers"),
                (EntityColumns.CustomerAliased, "Customers c"),
                (EntityColumns.Driver, "Drivers"),
                (EntityColumns.Invoice, "Invoices"),
                (EntityColumns.InvoiceAliased, "Invoices i"),
                (EntityColumns.JobAssignment, "JobAssignments"),
                (EntityColumns.Job, "Jobs"),
                (EntityColumns.LocationTracking, "LocationTracking")

                // Payments is not listed: no migration creates the table (see the payments-table todo).
            ];

            foreach ((string columns, string source) in selections)
            {
                // WHERE 1 = 0 validates the names without reading any rows.
                int fieldCount = await FieldCountAsync($"SELECT {columns} FROM {source} WHERE 1 = 0");

                fieldCount.Should().Be(columns.Split(',').Length, $"{source} columns {columns}");
            }
        }

        /// <summary>
        ///     Tests that the joined job assignment query selects from the migrated schema.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task JobAssignmentJoined_SelectsFromTheMigratedSchema()
        {
            int fieldCount = await FieldCountAsync($"{EntityColumns.JobAssignmentJoined} WHERE 1 = 0");

            fieldCount.Should().Be(20);
        }

        private static async Task<int> FieldCountAsync(string sql)
        {
            await using SqlConnection connection = new(TestDatabaseHelper.GetConnectionString());
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using SqlCommand command = new(sql, connection);
            await using SqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            return reader.FieldCount;
        }
    }
}
