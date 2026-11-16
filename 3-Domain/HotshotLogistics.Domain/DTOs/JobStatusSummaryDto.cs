namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>Counts of jobs by status.</summary>
    /// <param name="Pending">Number of pending jobs.</param>
    /// <param name="Assigned">Number of assigned jobs.</param>
    /// <param name="EnRoute">Number of en route jobs.</param>
    /// <param name="Received">Number of received jobs.</param>
    public sealed record JobStatusSummaryDto(int Pending, int Assigned, int EnRoute, int Received);
}
