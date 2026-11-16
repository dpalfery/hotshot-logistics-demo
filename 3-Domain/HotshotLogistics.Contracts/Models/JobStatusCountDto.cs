namespace HotshotLogistics.Contracts.Models;

/// <summary>
///     The number of jobs in a status.
/// </summary>
/// <param name="Status">The status name.</param>
/// <param name="Count">The number of jobs with this status.</param>
public sealed record JobStatusCountDto(string Status, int Count);
