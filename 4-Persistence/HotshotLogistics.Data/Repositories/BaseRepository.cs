using System.Globalization;
using System.Text.RegularExpressions;
using HotshotLogistics.Contracts.Repositories;
using Microsoft.Data.SqlClient;

namespace HotshotLogistics.Data.Repositories
{
    /// <summary>
    ///     Base repository implementation using native ADO.NET.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    internal abstract partial class BaseRepository<T> : IRepository<T>
        where T : class
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BaseRepository{T}" /> class.
        /// </summary>
        /// <param name="connectionFactory">The factory that opens database connections.</param>
        protected BaseRepository(ISqlConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <summary>
        ///     Gets the factory that opens database connections.
        /// </summary>
        protected ISqlConnectionFactory ConnectionFactory { get; }

        /// <inheritdoc />
        public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            string tableName = FormatIdentifier(GetTableName());
            string primaryKeyColumn = FormatIdentifier(GetPrimaryKeyColumnName());
            string commandText = $"SELECT * FROM {tableName} WHERE {primaryKeyColumn} = @Id";

            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);

            await using SqlCommand command = new(commandText, connection);
            command.Parameters.AddWithValue("@Id", id);

            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapReaderToEntity(reader);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            string tableName = FormatIdentifier(GetTableName());
            string commandText = $"SELECT * FROM {tableName}";

            List<T> entities = new();

            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync();

            await using SqlCommand command = new(commandText, connection);
            await using SqlDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                entities.Add(MapReaderToEntity(reader));
            }

            return entities;
        }

        /// <inheritdoc />
        public async Task<T> AddAsync(T entity)
        {
            const string sqlTemplate = "INSERT INTO {0} ({1}) OUTPUT INSERTED.* VALUES ({2})";

            string tableName = FormatIdentifier(GetTableName());
            SqlParameter[] parameters = GetInsertParameters(entity);
            string columnNames = string.Join(", ",
                parameters.Select(p => FormatIdentifier(p.ParameterName.TrimStart('@'))));
            string valuePlaceholders = string.Join(", ", parameters.Select(p => p.ParameterName));

            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync();

            await using SqlCommand command =
                new(string.Format(CultureInfo.InvariantCulture, sqlTemplate, tableName, columnNames, valuePlaceholders),
                    connection);
            command.Parameters.AddRange(parameters);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToEntity(reader);
            }

            throw new InvalidOperationException("Failed to insert entity");
        }

        /// <inheritdoc />
        public async Task<T> UpdateAsync(T entity)
        {
            const string sqlTemplate = "UPDATE {0} SET {1} OUTPUT INSERTED.* WHERE {2} = @Id";

            string tableName = FormatIdentifier(GetTableName());
            SqlParameter[] parameters = GetUpdateParameters(entity);
            string primaryKeyColumn = FormatIdentifier(GetPrimaryKeyColumnName());
            string setClause = string.Join(", ", parameters
                .Where(p => !string.Equals(FormatIdentifier(p.ParameterName.TrimStart('@')), primaryKeyColumn,
                    StringComparison.OrdinalIgnoreCase))
                .Select(p => $"{FormatIdentifier(p.ParameterName.TrimStart('@'))} = {p.ParameterName}"));

            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync();

            await using SqlCommand command =
                new(string.Format(CultureInfo.InvariantCulture, sqlTemplate, tableName, setClause, primaryKeyColumn),
                    connection);
            command.Parameters.AddRange(parameters);

            await using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToEntity(reader);
            }

            throw new InvalidOperationException("Failed to update entity");
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(object id)
        {
            string tableName = FormatIdentifier(GetTableName());
            string primaryKeyColumn = FormatIdentifier(GetPrimaryKeyColumnName());
            string commandText = $"DELETE FROM {tableName} WHERE {primaryKeyColumn} = @Id";

            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync();

            await using SqlCommand command = new(commandText, connection);
            command.Parameters.AddWithValue("@Id", id);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(object id)
        {
            string tableName = FormatIdentifier(GetTableName());
            string primaryKeyColumn = FormatIdentifier(GetPrimaryKeyColumnName());
            string commandText = $"SELECT COUNT(1) FROM {tableName} WHERE {primaryKeyColumn} = @Id";

            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync();

            await using SqlCommand command = new(commandText, connection);
            command.Parameters.AddWithValue("@Id", id);

            object? scalar = await command.ExecuteScalarAsync();
            int count = scalar is null || scalar == DBNull.Value
                ? 0
                : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);

            return count > 0;
        }

        /// <summary>
        ///     Gets the table name for the entity.
        /// </summary>
        /// <returns>The table name.</returns>
        protected abstract string GetTableName();

        /// <summary>
        ///     Gets the primary key column name.
        /// </summary>
        /// <returns>The primary key column name.</returns>
        protected abstract string GetPrimaryKeyColumnName();

        /// <summary>
        ///     Maps a data reader to an entity.
        /// </summary>
        /// <param name="reader">The data reader.</param>
        /// <returns>The mapped entity.</returns>
        protected abstract T MapReaderToEntity(SqlDataReader reader);

        /// <summary>
        ///     Gets the parameters for insert operation.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <returns>The SQL parameters.</returns>
        protected abstract SqlParameter[] GetInsertParameters(T entity);

        /// <summary>
        ///     Gets the parameters for update operation.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <returns>The SQL parameters.</returns>
        protected abstract SqlParameter[] GetUpdateParameters(T entity);

        /// <summary>
        ///     Executes a custom SQL query and returns entities.
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of entities.</returns>
        protected async Task<IEnumerable<T>> ExecuteQueryAsync(string sql, SqlParameter[]? parameters = null,
            CancellationToken cancellationToken = default)
        {
            List<T> entities = new();

            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);

            await using SqlCommand command = new(sql, connection);
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                entities.Add(MapReaderToEntity(reader));
            }

            return entities;
        }

        /// <summary>
        ///     Executes a custom SQL command that doesn't return entities.
        /// </summary>
        /// <param name="sql">The SQL command.</param>
        /// <param name="parameters">The command parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        protected async Task<int> ExecuteNonQueryAsync(string sql, SqlParameter[]? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);

            await using SqlCommand command = new(sql, connection);
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        ///     Executes a custom SQL query that returns a scalar value.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The scalar result.</returns>
        protected async Task<TResult> ExecuteScalarAsync<TResult>(string sql, SqlParameter[]? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = await ConnectionFactory.OpenConnectionAsync(cancellationToken);

            await using SqlCommand command = new(sql, connection);
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            object? result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result == DBNull.Value)
            {
                return default!;
            }

            if (result is TResult typedResult)
            {
                return typedResult;
            }

            return (TResult)Convert.ChangeType(result, typeof(TResult), CultureInfo.InvariantCulture);
        }

        [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
        private static partial Regex SafeIdentifierRegex();

        private static string FormatIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException("Identifier cannot be null or whitespace.", nameof(identifier));
            }

            if (!SafeIdentifierRegex().IsMatch(identifier))
            {
                throw new ArgumentException($"Identifier '{identifier}' contains invalid characters.",
                    nameof(identifier));
            }

            return $"[{identifier}]";
        }
    }
}
