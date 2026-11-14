// <copyright file="IntegrationTestBase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.IntegrationTests
{
    /// <summary>
    ///     Base class for integration tests that sets up the web application factory and provides a test client.
    /// </summary>
    public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory<Program>>,
        IClassFixture<DatabaseTestFixture>, IAsyncLifetime, IDisposable
    {
        protected readonly HttpClient Client;
        protected readonly CustomWebApplicationFactory<Program> Factory;

        /// <summary>
        ///     Rows this test creates; removed (and verified gone) after the test, pass or fail.
        /// </summary>
        protected readonly TestRowCleanup Cleanup = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="IntegrationTestBase" /> class.
        /// </summary>
        /// <param name="factory">The web application factory.</param>
        protected IntegrationTestBase(CustomWebApplicationFactory<Program> factory)
        {
            Factory = factory;
            Client = Factory.CreateClient();
        }

        /// <inheritdoc />
        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        ///     Deletes the rows registered with <see cref="Cleanup" />; fails the test when any remain.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            await Cleanup.CleanUpAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets the database connection string for tests.
        /// </summary>
        /// <returns>The connection string.</returns>
        protected static string GetConnectionString()
        {
            return TestDatabaseHelper.GetConnectionString();
        }

        /// <summary>
        ///     Disposes the HTTP client.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(); false if called from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Client.Dispose();
            }
        }
    }
}
