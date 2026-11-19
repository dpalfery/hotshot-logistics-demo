// <copyright file="SqlConnectionFactoryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Data;
using FluentAssertions;
using HotshotLogistics.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HotshotLogistics.Tests.Persistence
{
    /// <summary>
    ///     Tests for <see cref="SqlConnectionFactory" />: configuration, ownership, cancellation and failures.
    /// </summary>
    public class SqlConnectionFactoryTests
    {
        // Nothing listens on port 1, so opening fails fast. No credentials are involved.
        private static readonly string s_unreachableConnectionString =
            "Server=127.0.0.1,1;Connect Timeout=2;TrustServerCertificate=True";

        /// <summary>
        ///     Tests that a missing connection string is refused when the factory is created.
        /// </summary>
        [Fact]
        public void Constructor_WithoutConnectionString_ThrowsArgumentNullException()
        {
            IConfiguration configuration = new ConfigurationBuilder().Build();

            Action create = () => _ = new SqlConnectionFactory(configuration);

            create.Should().Throw<ArgumentNullException>().WithMessage("*DefaultConnection*");
        }

        /// <summary>
        ///     Tests that a failed open surfaces the provider exception to the caller.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task OpenConnectionAsync_WithUnreachableServer_ThrowsSqlException()
        {
            SqlConnectionFactory factory = CreateFactory(s_unreachableConnectionString);

            await Assert.ThrowsAsync<SqlException>(() => factory.OpenConnectionAsync(TestContext.Current.CancellationToken));
        }

        /// <summary>
        ///     Tests that a cancelled token stops the open.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task OpenConnectionAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            SqlConnectionFactory factory = CreateFactory(s_unreachableConnectionString);
            using CancellationTokenSource cancellation = new();
            await cancellation.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => factory.OpenConnectionAsync(cancellation.Token));
        }

        /// <summary>
        ///     Tests that the factory returns an open connection the caller owns and can dispose.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task OpenConnectionAsync_WithDatabase_ReturnsOpenConnectionOwnedByCaller()
        {
            SqlConnectionFactory factory = CreateFactory(TestDatabaseHelper.GetConnectionString());

            SqlConnection connection = await factory.OpenConnectionAsync(TestContext.Current.CancellationToken);
            connection.State.Should().Be(ConnectionState.Open);

            await connection.DisposeAsync();
            connection.State.Should().Be(ConnectionState.Closed);
        }

        private static SqlConnectionFactory CreateFactory(string connectionString)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                })
                .Build();

            return new SqlConnectionFactory(configuration);
        }
    }
}
