using HotshotLogistics.Contracts.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HotshotLogistics.Data
{
    /// <summary>
    ///     Creates SQL Server connections from the <c>DefaultConnection</c> connection string.
    /// </summary>
    internal sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SqlConnectionFactory" /> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <exception cref="ArgumentNullException">Connection string 'DefaultConnection' is not configured.</exception>
        public SqlConnectionFactory(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new ArgumentNullException(nameof(configuration),
                                    "Connection string 'DefaultConnection' is required");
        }

        /// <inheritdoc />
        public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            SqlConnection connection = new(_connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
    }
}
