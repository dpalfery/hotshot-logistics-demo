namespace HotshotLogistics.Contracts.Models
{
    /// <summary>
    ///     Counts of jobs in each status.
    /// </summary>
    /// <param name="Pending">The number of pending jobs.</param>
    /// <param name="Assigned">The number of assigned jobs.</param>
    /// <param name="EnRoute">The number of en route jobs.</param>
    /// <param name="Received">The number of received jobs.</param>
    public sealed record JobStatusCountsDto(int Pending, int Assigned, int EnRoute, int Received);
}
