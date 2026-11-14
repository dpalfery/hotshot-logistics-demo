// <copyright file="TestRowCleanup.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text;
using Microsoft.Data.SqlClient;

namespace HotshotLogistics.IntegrationTests
{
    /// <summary>
    ///     Records the rows a test creates in the shared database and removes them in <see cref="CleanUpAsync" />,
    ///     which test classes call from <c>IAsyncLifetime.DisposeAsync</c> so it runs even when the test failed.
    ///     A delete that fails, or a row that is still present afterwards, is logged to the test output and
    ///     throws, so the test fails instead of leaving data behind. Deletes run child tables first.
    /// </summary>
    public sealed class TestRowCleanup
    {
        private readonly List<string> _customerIds = [];
        private readonly List<int> _driverIds = [];
        private readonly List<string> _driverEmails = [];
        private readonly List<string> _invoiceIds = [];
        private readonly List<string> _jobIds = [];
        private readonly List<string> _locationJobIds = [];

        /// <summary>
        ///     Tracks a customer row (its contacts go with it through the foreign key cascade).
        /// </summary>
        public void TrackCustomer(string id) => _customerIds.Add(id);

        /// <summary>
        ///     Tracks a driver row by identity id (its job assignments are removed first).
        /// </summary>
        public void TrackDriver(int id) => _driverIds.Add(id);

        /// <summary>
        ///     Tracks a driver row by email, for when the identity id is not known.
        /// </summary>
        public void TrackDriverEmail(string email) => _driverEmails.Add(email);

        /// <summary>
        ///     Tracks an invoice row (its line items go with it through the foreign key cascade).
        /// </summary>
        public void TrackInvoice(string id) => _invoiceIds.Add(id);

        /// <summary>
        ///     Tracks a job row together with its assignments and invoices.
        /// </summary>
        public void TrackJob(string id) => _jobIds.Add(id);

        /// <summary>
        ///     Tracks every location tracking row recorded for this job id.
        /// </summary>
        public void TrackLocationJob(string jobId) => _locationJobIds.Add(jobId);

        /// <summary>
        ///     Deletes everything tracked, then verifies nothing is left.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a delete failed or a tracked row remains.</exception>
        public async Task CleanUpAsync()
        {
            (string Table, string Column, IReadOnlyList<object> Values)[] steps =
            [
                ("LocationTracking", "JobId", _locationJobIds),
                ("Invoices", "Id", _invoiceIds),
                ("Invoices", "JobId", _jobIds),
                ("JobAssignments", "JobId", _jobIds),
                ("JobAssignments", "DriverId", _driverIds.Cast<object>().ToList()),
                ("Jobs", "Id", _jobIds),
                ("Drivers", "Id", _driverIds.Cast<object>().ToList()),
                ("Drivers", "Email", _driverEmails),
                ("Customers", "Id", _customerIds)
            ];

            if (steps.All(s => s.Values.Count == 0) || !TestDatabaseHelper.IsConfigured)
            {
                return;
            }

            List<string> problems = [];
            await using SqlConnection connection = new(TestDatabaseHelper.GetConnectionString());
            await connection.OpenAsync();

            foreach ((string table, string column, IReadOnlyList<object> values) in steps.Where(s => s.Values.Count > 0))
            {
                try
                {
                    int deleted = await ExecuteAsync(connection, "DELETE FROM", table, column, values);
                    Log($"cleanup: deleted {deleted} row(s) from {table} by {column}");
                }
                catch (Exception ex)
                {
                    problems.Add($"delete from {table} by {column} failed: {ex.Message}");
                }
            }

            foreach ((string table, string column, IReadOnlyList<object> values) in steps.Where(s => s.Values.Count > 0))
            {
                try
                {
                    int remaining = await ExecuteAsync(connection, "SELECT COUNT(*) FROM", table, column, values);
                    if (remaining > 0)
                    {
                        problems.Add($"{remaining} row(s) remain in {table} for {column} in [{string.Join(", ", values)}]");
                    }
                }
                catch (Exception ex)
                {
                    problems.Add($"verify {table} by {column} failed: {ex.Message}");
                }
            }

            if (problems.Count > 0)
            {
                foreach (string problem in problems)
                {
                    Log($"cleanup FAILED: {problem}");
                }

                throw new InvalidOperationException("Test data cleanup failed: " + string.Join("; ", problems));
            }
        }

        private static async Task<int> ExecuteAsync(
            SqlConnection connection,
            string verb,
            string table,
            string column,
            IReadOnlyList<object> values)
        {
            await using SqlCommand command = connection.CreateCommand();
            StringBuilder names = new();
            for (int i = 0; i < values.Count; i++)
            {
                string name = $"@p{i}";
                _ = names.Append(i == 0 ? name : "," + name);
                _ = command.Parameters.AddWithValue(name, values[i]);
            }

            command.CommandText = $"{verb} {table} WHERE {column} IN ({names})";
            return verb.StartsWith("SELECT", StringComparison.Ordinal)
                ? (int)(await command.ExecuteScalarAsync() ?? 0)
                : await command.ExecuteNonQueryAsync();
        }

        private static void Log(string message)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(message);
        }
    }
}
