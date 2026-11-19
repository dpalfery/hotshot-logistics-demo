using Microsoft.Data.SqlClient;

namespace HotshotLogistics.Contracts.Repositories
{
    /// <summary>
    ///     Creates and opens SQL Server connections for repositories.
    /// </summary>
    public interface ISqlConnectionFactory
    {
        /// <summary>
        ///     Creates a connection and opens it. The caller owns the returned connection and must dispose it.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An open connection.</returns>
        /// <exception cref="SqlException">The connection could not be opened.</exception>
        /// <exception cref="OperationCanceledException">The token was cancelled before the connection opened.</exception>
        Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
    }
}
