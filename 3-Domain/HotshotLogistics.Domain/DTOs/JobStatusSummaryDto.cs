namespace HotshotLogistics.Domain.DTOs
{
    // Decision D5: keep this shared DTO here until the Contracts.Models target exists;
    // migration is tracked by the contracts-model-placement todo.
    /// <summary>Counts of jobs by status.</summary>
    /// <param name="Pending">Number of pending jobs.</param>
    /// <param name="Assigned">Number of assigned jobs.</param>
    /// <param name="EnRoute">Number of en route jobs.</param>
    /// <param name="Received">Number of received jobs.</param>
    public sealed record JobStatusSummaryDto(int Pending, int Assigned, int EnRoute, int Received);
}
