using System.Data;
using System.Globalization;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace HotshotLogistics.Data.Repositories
{
    /// <summary>
    ///     Repository implementation for JobAssignment operations using native ADO.NET.
    /// </summary>
    internal class JobAssignmentRepository : BaseRepository<JobAssignmentDto>, IJobAssignmentRepository
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="JobAssignmentRepository" /> class.
        /// </summary>
        /// <param name="connectionFactory">The factory that opens database connections.</param>
        public JobAssignmentRepository(ISqlConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        /// <inheritdoc />
        public override async Task<JobAssignmentDto?> GetByIdAsync(object id,
            CancellationToken cancellationToken = default)
        {
            const string sql = $@"
            {EntityColumns.JobAssignmentJoined}
            WHERE ja.Id = @Id";

            SqlParameter[] parameters =
                [new("@Id", SqlDbType.NVarChar) { Value = Convert.ToString(id, CultureInfo.InvariantCulture) }];
            IEnumerable<JobAssignmentDto> assignments = await ExecuteQueryAsync(sql, parameters, cancellationToken);
            return assignments.FirstOrDefault();
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<JobAssignmentDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            const string sql = $@"
            {EntityColumns.JobAssignmentJoined}
            ORDER BY ja.AssignedAt DESC";

            return await ExecuteQueryAsync(sql, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<JobAssignmentDto>> GetByDriverIdAsync(int driverId,
            CancellationToken cancellationToken = default)
        {
            const string sql = $@"
            {EntityColumns.JobAssignmentJoined}
            WHERE ja.DriverId = @DriverId
            ORDER BY ja.AssignedAt DESC";

            SqlParameter[] parameters = [new("@DriverId", SqlDbType.Int) { Value = driverId }];
            return await ExecuteQueryAsync(sql, parameters, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<JobAssignmentDto>> GetByJobIdAsync(string jobId,
            CancellationToken cancellationToken = default)
        {
            const string sql = $@"
            {EntityColumns.JobAssignmentJoined}
            WHERE ja.JobId = @JobId
            ORDER BY ja.AssignedAt DESC";

            SqlParameter[] parameters = [new("@JobId", SqlDbType.NVarChar) { Value = jobId }];
            return await ExecuteQueryAsync(sql, parameters, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<JobAssignmentDto>> GetActiveAssignmentsAsync(
            CancellationToken cancellationToken = default)
        {
            const string sql = $@"
            {EntityColumns.JobAssignmentJoined}
            WHERE ja.Status = @Status
            ORDER BY ja.AssignedAt DESC";

            SqlParameter[] parameters = [new("@Status", SqlDbType.Int) { Value = (int)JobAssignmentStatus.Active }];
            return await ExecuteQueryAsync(sql, parameters, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<JobAssignmentDto> CreateAsync(JobAssignmentDto jobAssignment,
            CancellationToken cancellationToken = default)
        {
            if (jobAssignment == null)
            {
                throw new ArgumentNullException(nameof(jobAssignment));
            }

            jobAssignment.Id = Guid.NewGuid().ToString();
            return await AddAsync(jobAssignment, cancellationToken);
        }

        /// <inheritdoc />
        public override async Task<JobAssignmentDto> UpdateAsync(JobAssignmentDto jobAssignment,
            CancellationToken cancellationToken = default)
        {
            if (jobAssignment == null)
            {
                throw new ArgumentNullException(nameof(jobAssignment));
            }

            return await base.UpdateAsync(jobAssignment, cancellationToken);
        }

        /// <inheritdoc />
        protected override string GetColumnList()
        {
            return EntityColumns.JobAssignment;
        }

        /// <inheritdoc />
        protected override string GetTableName()
        {
            return "JobAssignments";
        }

        /// <inheritdoc />
        protected override string GetPrimaryKeyColumnName()
        {
            return "Id";
        }

        /// <inheritdoc />
        protected override JobAssignmentDto MapReaderToEntity(SqlDataReader reader)
        {
            // Ensure all required columns are checked for DBNull before accessing
            return new JobAssignmentDto
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                JobId = reader.GetString(reader.GetOrdinal("JobId")),
                DriverId = reader.GetInt32(reader.GetOrdinal("DriverId")),
                AssignedAt = reader.GetDateTime(reader.GetOrdinal("AssignedAt")),
                Status = (JobAssignmentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Driver = reader.IsDBNull(reader.GetOrdinal("DriverFirstName"))
                    ? null
                    : new DriverDto
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("DriverId")),
                        FirstName = reader.GetString(reader.GetOrdinal("DriverFirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("DriverLastName")),
                        Email = reader.GetString(reader.GetOrdinal("DriverEmail")),
                        PhoneNumber = reader.GetString(reader.GetOrdinal("DriverPhoneNumber")),
                        LicenseNumber = reader.GetString(reader.GetOrdinal("DriverLicenseNumber")),
                        LicenseExpiryDate = reader.GetDateTime(reader.GetOrdinal("DriverLicenseExpiryDate"))
                    },
                Job = reader.IsDBNull(reader.GetOrdinal("JobTitle"))
                    ? null
                    : new Job
                    {
                        Id = reader.GetString(reader.GetOrdinal("JobId")),
                        Title = reader.GetString(reader.GetOrdinal("JobTitle")),
                        PickupLocation = new Location
                        { Address = reader.GetString(reader.GetOrdinal("JobPickupAddress")) },
                        DeliveryLocation = new Location
                        { Address = reader.GetString(reader.GetOrdinal("JobDeliveryAddress")) },
                        Status = (JobStatus)reader.GetInt32(reader.GetOrdinal("JobStatus")),
                        Priority = (JobPriority)reader.GetInt32(reader.GetOrdinal("JobPriority")),
                        Amount = reader.GetDecimal(reader.GetOrdinal("JobAmount")),
                        EstimatedDeliveryTime = reader.GetDateTime(reader.GetOrdinal("JobEstimatedDeliveryTime")),
                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("JobCreatedAt")),
                        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("JobUpdatedAt"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("JobUpdatedAt"))
                    }
            };
        }

        /// <inheritdoc />
        protected override SqlParameter[] GetInsertParameters(JobAssignmentDto entity)
        {
            return
            [
                new SqlParameter("@Id", SqlDbType.NVarChar) { Value = entity.Id },
                new SqlParameter("@JobId", SqlDbType.NVarChar) { Value = entity.JobId },
                new SqlParameter("@DriverId", SqlDbType.Int) { Value = entity.DriverId },
                new SqlParameter("@AssignedAt", SqlDbType.DateTime2) { Value = entity.AssignedAt },
                new SqlParameter("@Status", SqlDbType.Int) { Value = (int)entity.Status }
            ];
        }

        /// <inheritdoc />
        protected override SqlParameter[] GetUpdateParameters(JobAssignmentDto entity)
        {
            return
            [
                new SqlParameter("@Id", SqlDbType.NVarChar) { Value = entity.Id },
                new SqlParameter("@JobId", SqlDbType.NVarChar) { Value = entity.JobId },
                new SqlParameter("@DriverId", SqlDbType.Int) { Value = entity.DriverId },
                new SqlParameter("@AssignedAt", SqlDbType.DateTime2) { Value = entity.AssignedAt },
                new SqlParameter("@Status", SqlDbType.Int) { Value = (int)entity.Status }
            ];
        }
    }
}
