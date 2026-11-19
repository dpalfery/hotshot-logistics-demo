namespace HotshotLogistics.Application.Hubs
{
    /// <summary>
    ///     Names of the SignalR groups on the <c>/realtime</c> hub, so the sender and the hub agree on them.
    /// </summary>
    public static class RealtimeGroups
    {
        /// <summary>
        ///     The admin dashboard audience. The hub adds a connection to it on connect only when the
        ///     caller satisfies the <c>ManagerOrAdmin</c> policy.
        /// </summary>
        public const string Admins = "admins";

        /// <summary>
        ///     Gets the group of the clients tracking one job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <returns>The group name.</returns>
        public static string Job(string jobId)
        {
            return $"job-{jobId}";
        }
    }
}
