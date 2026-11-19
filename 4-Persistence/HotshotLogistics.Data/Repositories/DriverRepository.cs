// <copyright file="DriverRepository.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Data;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Data.SqlClient;

namespace HotshotLogistics.Data.Repositories
{
    /// <summary>
    ///     Repository for managing Driver entities using native ADO.NET.
    /// </summary>
    internal class DriverRepository : BaseRepository<Driver>, IDriverRepository
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DriverRepository" /> class.
        /// </summary>
        /// <param name="connectionFactory">The factory that opens database connections.</param>
        public DriverRepository(ISqlConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Driver>> GetDriversAsync(CancellationToken cancellationToken = default)
        {
            const string sql = $"SELECT {EntityColumns.Driver} FROM Drivers WHERE IsActive = 1 ORDER BY LastName, FirstName";
            IEnumerable<Driver> drivers = await ExecuteQueryAsync(sql, null, cancellationToken);
            return drivers;
        }

        /// <inheritdoc />
        public async Task<Driver?> GetDriverByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = $"SELECT {EntityColumns.Driver} FROM Drivers WHERE Id = @Id";
            SqlParameter[] parameters = [new("@Id", SqlDbType.Int) { Value = id }];
            IEnumerable<Driver> drivers = await ExecuteQueryAsync(sql, parameters, cancellationToken);
            return drivers.FirstOrDefault();
        }

        /// <inheritdoc />
        public async Task<Driver> CreateDriverAsync(Driver driver, CancellationToken cancellationToken = default)
        {
            return await AddAsync(driver, cancellationToken);
        }

        /// <inheritdoc />
        public override async Task<Driver> AddAsync(Driver driver, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(driver);

            driver.CreatedAt = DateTime.UtcNow;
            return await base.AddAsync(driver, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Driver> UpdateDriverAsync(Driver driver, CancellationToken cancellationToken = default)
        {
            return await UpdateAsync(driver, cancellationToken);
        }

        /// <inheritdoc />
        public override async Task<Driver> UpdateAsync(Driver driver, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(driver);

            driver.UpdatedAt = DateTime.UtcNow;
            return await base.UpdateAsync(driver, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteDriverAsync(int id, CancellationToken cancellationToken = default)
        {
            // Soft delete - mark as inactive
            const string sql = "UPDATE Drivers SET IsActive = 0, UpdatedAt = @UpdatedAt WHERE Id = @Id";
            SqlParameter[] parameters =
            [
                new("@Id", SqlDbType.Int) { Value = id },
                new("@UpdatedAt", SqlDbType.DateTime2) { Value = DateTime.UtcNow }
            ];
            int rowsAffected = await ExecuteNonQueryAsync(sql, parameters, cancellationToken);
            return rowsAffected > 0;
        }

        /// <summary>
        ///     Gets all active drivers.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of active drivers.</returns>
        public async Task<IEnumerable<Driver>> GetActiveDriversAsync(CancellationToken cancellationToken = default)
        {
            const string sql = $"SELECT {EntityColumns.Driver} FROM Drivers WHERE IsActive = 1 ORDER BY LastName, FirstName";
            IEnumerable<Driver> drivers = await ExecuteQueryAsync(sql, null, cancellationToken);
            return drivers;
        }

        /// <summary>
        ///     Gets a driver by license number.
        /// </summary>
        /// <param name="licenseNumber">The driver's license number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The driver if found; otherwise, null.</returns>
        public async Task<Driver?> GetDriverByLicenseNumberAsync(string licenseNumber,
            CancellationToken cancellationToken = default)
        {
            const string sql = $"SELECT {EntityColumns.Driver} FROM Drivers WHERE LicenseNumber = @LicenseNumber";
            SqlParameter[] parameters =
                [new("@LicenseNumber", SqlDbType.NVarChar) { Value = licenseNumber }];
            IEnumerable<Driver> drivers = await ExecuteQueryAsync(sql, parameters, cancellationToken);
            return drivers.FirstOrDefault();
        }

        /// <summary>
        ///     Gets drivers by status.
        /// </summary>
        /// <param name="status">The driver status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A collection of drivers with the specified status.</returns>
        public async Task<IEnumerable<Driver>> GetDriversByStatusAsync(DriverStatus status,
            CancellationToken cancellationToken = default)
        {
            // Note: CurrentStatus is not stored in the database, returning all active drivers
            const string sql = $"SELECT {EntityColumns.Driver} FROM Drivers WHERE IsActive = 1 ORDER BY LastName, FirstName";
            IEnumerable<Driver> drivers = await ExecuteQueryAsync(sql, null, cancellationToken);
            return drivers;
        }

        /// <inheritdoc />
        protected override string GetColumnList()
        {
            return EntityColumns.Driver;
        }

        /// <inheritdoc />
        protected override string GetTableName()
        {
            return "Drivers";
        }

        /// <inheritdoc />
        protected override string GetPrimaryKeyColumnName()
        {
            return "Id";
        }

        /// <inheritdoc />
        protected override Driver MapReaderToEntity(SqlDataReader reader)
        {
            return new Driver
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                PersonalInfo = new PersonalInfo
                {
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber"))
                        ? string.Empty
                        : reader.GetString(reader.GetOrdinal("PhoneNumber"))
                },
                License = new LicenseInfo
                {
                    LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber")),
                    LicenseExpiryDate = reader.IsDBNull(reader.GetOrdinal("LicenseExpiryDate"))
                        ? DateTime.UtcNow.AddYears(1) // Default to 1 year from now if NULL
                        : reader.GetDateTime(reader.GetOrdinal("LicenseExpiryDate"))
                },
                CurrentStatus = (DriverStatus)reader.GetInt32(reader.GetOrdinal("CurrentStatus")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        /// <inheritdoc />
        protected override SqlParameter[] GetInsertParameters(Driver entity)
        {
            return
            [
                new SqlParameter("@FirstName", SqlDbType.NVarChar) { Value = entity.PersonalInfo.FirstName },
                new SqlParameter("@LastName", SqlDbType.NVarChar) { Value = entity.PersonalInfo.LastName },
                new SqlParameter("@Email", SqlDbType.NVarChar) { Value = entity.PersonalInfo.Email },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar)
                    { Value = (object?)entity.PersonalInfo.PhoneNumber ?? DBNull.Value },
                new SqlParameter("@LicenseNumber", SqlDbType.NVarChar) { Value = entity.License.LicenseNumber },
                new SqlParameter("@LicenseState", SqlDbType.NVarChar)
                    { Value = (object?)entity.License.LicenseState ?? DBNull.Value },
                new SqlParameter("@LicenseExpiryDate", SqlDbType.Date) { Value = entity.License.LicenseExpiryDate },
                new SqlParameter("@InsuranceExpiryDate", SqlDbType.Date)
                {
                    Value = entity.Vehicle.InsuranceExpiryDate != DateTime.MinValue
                        ? entity.Vehicle.InsuranceExpiryDate
                        : DBNull.Value
                },
                new SqlParameter("@CurrentStatus", SqlDbType.Int) { Value = (int)entity.CurrentStatus },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = entity.IsActive }
            ];
        }

        /// <inheritdoc />
        protected override SqlParameter[] GetUpdateParameters(Driver entity)
        {
            return
            [
                new SqlParameter("@Id", SqlDbType.Int) { Value = entity.Id },
                new SqlParameter("@FirstName", SqlDbType.NVarChar) { Value = entity.PersonalInfo.FirstName },
                new SqlParameter("@LastName", SqlDbType.NVarChar) { Value = entity.PersonalInfo.LastName },
                new SqlParameter("@Email", SqlDbType.NVarChar) { Value = entity.PersonalInfo.Email },
                new SqlParameter("@PhoneNumber", SqlDbType.NVarChar)
                    { Value = (object?)entity.PersonalInfo.PhoneNumber ?? DBNull.Value },
                new SqlParameter("@LicenseNumber", SqlDbType.NVarChar) { Value = entity.License.LicenseNumber },
                new SqlParameter("@LicenseState", SqlDbType.NVarChar)
                    { Value = (object?)entity.License.LicenseState ?? DBNull.Value },
                new SqlParameter("@LicenseExpiryDate", SqlDbType.Date) { Value = entity.License.LicenseExpiryDate },
                new SqlParameter("@InsuranceExpiryDate", SqlDbType.Date)
                {
                    Value = entity.Vehicle.InsuranceExpiryDate != DateTime.MinValue
                        ? entity.Vehicle.InsuranceExpiryDate
                        : DBNull.Value
                },
                new SqlParameter("@CurrentStatus", SqlDbType.Int) { Value = (int)entity.CurrentStatus },
                new SqlParameter("@IsActive", SqlDbType.Bit) { Value = entity.IsActive }
            ];
        }
    }
}
