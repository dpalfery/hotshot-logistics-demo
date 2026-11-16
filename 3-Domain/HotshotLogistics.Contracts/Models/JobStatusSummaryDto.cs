namespace HotshotLogistics.Contracts.Models
{
    /// <summary>Counts of jobs in each status.</summary>
    /// <param name="Pending">Pending jobs.</param>
    /// <param name="Assigned">Assigned jobs.</param>
    /// <param name="EnRoute">Jobs en route.</param>
    /// <param name="Received">Received jobs.</param>
    public sealed record JobStatusSummaryDto(int Pending, int Assigned, int EnRoute, int Received);
}
