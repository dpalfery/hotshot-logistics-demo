using FluentAssertions;
using HotshotLogistics.Application.Hubs;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Contracts.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotshotLogistics.Tests.Communication
{
    /// <summary>
    ///     Runs <see cref="RealtimeService" /> through the real SignalR hub context, so the typed client proxy
    ///     is exercised, and records what reaches the hub lifetime manager: the group, the client method and
    ///     its arguments, which is what a connected client would receive.
    /// </summary>
    public class RealtimeBroadcastWireTests
    {
        private readonly RecordingHubLifetimeManager _wire = new();
        private readonly RealtimeService _service;

        public RealtimeBroadcastWireTests()
        {
            ServiceCollection services = new();
            services.AddLogging();
            services.AddSignalR();
            services.AddSingleton<HubLifetimeManager<RealtimeHub>>(_wire);
            IServiceProvider provider = services.BuildServiceProvider();

            SignalRClientWrapper client = new(
                provider.GetRequiredService<IHubContext<RealtimeHub, IRealtimeHubClient>>());
            _service = new RealtimeService(client, NullLogger<RealtimeService>.Instance);
        }

        [Fact]
        public async Task BroadcastJobStatusUpdate_DeliversJobIdAndStatusToTheAdminsGroupExactlyOnce()
        {
            await _service.BroadcastJobStatusUpdate("job-42", JobStatus.EnRoute);

            _wire.Sent.Where(m => m.Group == RealtimeGroups.Admins).Should().ContainSingle()
                .Which.Should().Be(new RecordedSend("admins", "JobStatusUpdated", ["job-42", JobStatus.EnRoute]),
                    "the dashboard handler takes the job id and the status");
        }

        [Fact]
        public async Task BroadcastJobStatusUpdate_AlsoDeliversToTheJobGroupAndNowhereElse()
        {
            await _service.BroadcastJobStatusUpdate("job-42", JobStatus.Received);

            _wire.Sent.Select(m => m.Group).Should().BeEquivalentTo(["admins", "job-job-42"]);
            _wire.Sent.Should().OnlyContain(m => m.Method == "JobStatusUpdated");
        }
    }

    internal sealed record RecordedSend(string Group, string Method, object?[] Args)
    {
        public bool Equals(RecordedSend? other)
        {
            return other is not null && Group == other.Group && Method == other.Method &&
                   Args.SequenceEqual(other.Args);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Group, Method, Args.Length);
        }
    }

    /// <summary>
    ///     A hub lifetime manager with no connections that records every group send.
    /// </summary>
    internal sealed class RecordingHubLifetimeManager : HubLifetimeManager<RealtimeHub>
    {
        public List<RecordedSend> Sent { get; } = [];

        public override Task SendGroupAsync(string groupName, string methodName, object?[] args,
            CancellationToken cancellationToken = default)
        {
            Sent.Add(new RecordedSend(groupName, methodName, args));
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync(HubConnectionContext connection) => Task.CompletedTask;

        public override Task OnDisconnectedAsync(HubConnectionContext connection) => Task.CompletedTask;

        public override Task SendAllAsync(string methodName, object?[] args,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task SendAllExceptAsync(string methodName, object?[] args,
            IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public override Task SendConnectionAsync(string connectionId, string methodName, object?[] args,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName,
            object?[] args, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task SendGroupExceptAsync(string groupName, string methodName, object?[] args,
            IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public override Task SendUserAsync(string userId, string methodName, object?[] args,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task AddToGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public override Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
