using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HotshotLogistics.Application.Authorization;
using HotshotLogistics.Application.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Who may open the <c>/realtime</c> hub and who is placed in the admin dashboard audience, through the
    ///     real pipeline: the negotiate request, then the WebSocket a browser opens with the token in the query
    ///     string. Development hosting uses the <c>Test</c> scheme, so no identity provider or database is needed.
    /// </summary>
    public class RealtimeHubAuthenticationTests
    {
        private const string s_negotiateEndpoint = "/realtime/negotiate?negotiateVersion=1";
        private const char s_recordSeparator = '\u001e';
        private const string s_roleHeader = "X-Test-Role";
        private const string s_roundTripJobId = "round-trip";

        [Fact]
        public async Task Negotiate_WithoutCredentials_Returns401()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory(new GroupRecordingLifetimeManager());
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await Negotiate(client);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Negotiate_WithAnInvalidBearerToken_Returns401()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory(new GroupRecordingLifetimeManager());
            using HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

            using HttpResponseMessage response = await Negotiate(client);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("Test", null)]
        [InlineData("Bearer", "test-token")]
        public async Task Negotiate_WithTheDevelopmentToken_Returns200(string scheme, string? parameter)
        {
            await using WebApplicationFactory<Program> factory = CreateFactory(new GroupRecordingLifetimeManager());
            using HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, parameter);

            using HttpResponseMessage response = await Negotiate(client);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Connect_WithoutCredentials_IsRefusedAndJoinsNoGroup()
        {
            GroupRecordingLifetimeManager wire = new();
            await using WebApplicationFactory<Program> factory = CreateFactory(wire);

            Func<Task> act = () => OpenSocketAsync(factory, "/realtime?id=anonymous", null);

            await act.Should().ThrowAsync<InvalidOperationException>();
            wire.GroupJoins.Should().BeEmpty();
        }

        [Theory]
        [InlineData(AuthorizationPolicies.Admin)]
        [InlineData(AuthorizationPolicies.Manager)]
        public async Task Connect_AsAManagerOrAdmin_JoinsTheAdminDashboardGroup(string role)
        {
            GroupRecordingLifetimeManager wire = new();
            await using WebApplicationFactory<Program> factory = CreateFactory(wire);
            string connectionToken = await NegotiateAsync(factory, role);

            using WebSocket socket = await OpenSocketAsync(factory, $"/realtime?id={connectionToken}", role);
            await HandshakeAsync(socket);

            await RoundTripAsync(socket, wire);
            wire.GroupJoins.Select(join => join.Group).Should().BeEquivalentTo(
                [RealtimeGroups.Admins, RealtimeGroups.Job(s_roundTripJobId)]);
        }

        [Theory]
        [InlineData(AuthorizationPolicies.Driver)]
        [InlineData(AuthorizationPolicies.Customer)]
        public async Task Connect_AsAnotherRole_IsAcceptedButNotInTheAdminDashboardGroup(string role)
        {
            GroupRecordingLifetimeManager wire = new();
            await using WebApplicationFactory<Program> factory = CreateFactory(wire);
            string connectionToken = await NegotiateAsync(factory, role);

            using WebSocket socket = await OpenSocketAsync(factory, $"/realtime?id={connectionToken}", role);
            await HandshakeAsync(socket);

            await RoundTripAsync(socket, wire);
            wire.GroupJoins.Select(join => join.Group).Should().BeEquivalentTo(
                [RealtimeGroups.Job(s_roundTripJobId)], "only the round trip's own group join is expected");
        }

        [Fact]
        public async Task Connect_WithTheTokenInTheQueryString_AuthenticatesLikeABrowserWebSocket()
        {
            GroupRecordingLifetimeManager wire = new();
            await using WebApplicationFactory<Program> factory = CreateFactory(wire);
            string connectionToken = await NegotiateAsync(factory, AuthorizationPolicies.Admin);

            using WebSocket socket = await OpenSocketAsync(
                factory, $"/realtime?id={connectionToken}&access_token=test-token", null);
            await HandshakeAsync(socket);

            await RoundTripAsync(socket, wire);
            wire.GroupJoins.Select(join => join.Group).Should().BeEquivalentTo(
                [RealtimeGroups.Admins, RealtimeGroups.Job(s_roundTripJobId)]);
        }

        [Theory]
        [InlineData("/realtime", "signed-jwt", "signed-jwt")]
        [InlineData("/api/Job", "signed-jwt", null)]
        public async Task JwtBearer_OutsideDevelopment_TakesTheQueryTokenOnlyOnTheHubPath(
            string path, string queryToken, string? expectedToken)
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create(
                new Dictionary<string, string?>
                {
                    ["AzureAdB2C:Instance"] = "https://example.invalid/",
                    ["AzureAdB2C:Domain"] = "example.invalid",
                    ["AzureAdB2C:TenantId"] = "00000000-0000-0000-0000-000000000000",
                    ["AzureAdB2C:ClientId"] = "00000000-0000-0000-0000-000000000000",
                    ["AzureAdB2C:SignUpSignInPolicyId"] = "B2C_1_signupsignin",
                },
                environment: "Production");
            JwtBearerOptions options = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);
            DefaultHttpContext httpContext = new();
            httpContext.Request.Path = path;
            httpContext.Request.QueryString = new QueryString($"?access_token={queryToken}");
            MessageReceivedContext context = new(
                httpContext,
                new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
                options);

            await options.Events.OnMessageReceived(context);

            context.Token.Should().Be(expectedToken);
        }

        private static WebApplicationFactory<Program> CreateFactory(GroupRecordingLifetimeManager wire)
        {
            return ApiHostFactory.Create(configureServices: services =>
                services.ReplaceSingleton<HubLifetimeManager<RealtimeHub>>(wire));
        }

        private static Task<HttpResponseMessage> Negotiate(HttpClient client)
        {
            return client.PostAsync(s_negotiateEndpoint, null, TestContext.Current.CancellationToken);
        }

        private static async Task<string> NegotiateAsync(WebApplicationFactory<Program> factory, string role)
        {
            using HttpClient client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
            client.DefaultRequestHeaders.Add(s_roleHeader, role);

            using HttpResponseMessage response = await Negotiate(client);
            response.EnsureSuccessStatusCode();
            using JsonDocument body = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            return body.RootElement.GetProperty("connectionToken").GetString()!;
        }

        private static async Task<WebSocket> OpenSocketAsync(
            WebApplicationFactory<Program> factory, string pathAndQuery, string? role)
        {
            WebSocketClient webSocketClient = factory.Server.CreateWebSocketClient();
            webSocketClient.ConfigureRequest = request =>
            {
                if (role is not null)
                {
                    request.Headers["Authorization"] = "Test";
                    request.Headers[s_roleHeader] = role;
                }
            };

            return await webSocketClient.ConnectAsync(
                new Uri(factory.Server.BaseAddress, pathAndQuery), TestContext.Current.CancellationToken);
        }

        /// <summary>
        ///     Invokes a hub method and waits for its effect. The hub runs the invocation only after its
        ///     <c>OnConnectedAsync</c> has finished, so everything that method does on connect has happened
        ///     once this returns.
        /// </summary>
        private static async Task RoundTripAsync(WebSocket socket, GroupRecordingLifetimeManager wire)
        {
            string invocation =
                $"{{\"type\":1,\"invocationId\":\"1\",\"target\":\"JoinJobTracking\"," +
                $"\"arguments\":[\"{s_roundTripJobId}\"]}}{s_recordSeparator}";
            await socket.SendAsync(Encoding.UTF8.GetBytes(invocation), WebSocketMessageType.Text, true,
                TestContext.Current.CancellationToken);

            await wire.WaitForJoinAsync(RealtimeGroups.Job(s_roundTripJobId));
        }

        private static async Task HandshakeAsync(WebSocket socket)
        {
            byte[] request = Encoding.UTF8.GetBytes($"{{\"protocol\":\"json\",\"version\":1}}{s_recordSeparator}");
            await socket.SendAsync(request, WebSocketMessageType.Text, true, TestContext.Current.CancellationToken);

            byte[] buffer = new byte[256];
            WebSocketReceiveResult result =
                await socket.ReceiveAsync(buffer, TestContext.Current.CancellationToken);
            Encoding.UTF8.GetString(buffer, 0, result.Count).Should().Be($"{{}}{s_recordSeparator}");
        }
    }

    /// <summary>
    ///     The real in-memory hub lifetime manager, which also records every group join so a test can see who
    ///     was placed in which group.
    /// </summary>
    internal sealed class GroupRecordingLifetimeManager : HubLifetimeManager<RealtimeHub>
    {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

        private readonly DefaultHubLifetimeManager<RealtimeHub> _inner =
            new(Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultHubLifetimeManager<RealtimeHub>>.Instance);

        private readonly List<(string ConnectionId, string Group)> _groupJoins = [];

        public IReadOnlyList<(string ConnectionId, string Group)> GroupJoins
        {
            get
            {
                lock (_groupJoins)
                {
                    return [.. _groupJoins];
                }
            }
        }

        public async Task WaitForJoinAsync(string group)
        {
            using CancellationTokenSource timeout = new(s_timeout);
            while (!GroupJoins.Any(join => join.Group == group))
            {
                await Task.Delay(10, timeout.Token);
            }
        }

        public override Task AddToGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = default)
        {
            lock (_groupJoins)
            {
                _groupJoins.Add((connectionId, groupName));
            }

            return _inner.AddToGroupAsync(connectionId, groupName, cancellationToken);
        }

        public override Task OnConnectedAsync(HubConnectionContext connection) => _inner.OnConnectedAsync(connection);

        public override Task OnDisconnectedAsync(HubConnectionContext connection) =>
            _inner.OnDisconnectedAsync(connection);

        public override Task SendAllAsync(string methodName, object?[] args,
            CancellationToken cancellationToken = default) => _inner.SendAllAsync(methodName, args, cancellationToken);

        public override Task SendAllExceptAsync(string methodName, object?[] args,
            IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default) =>
            _inner.SendAllExceptAsync(methodName, args, excludedConnectionIds, cancellationToken);

        public override Task SendConnectionAsync(string connectionId, string methodName, object?[] args,
            CancellationToken cancellationToken = default) =>
            _inner.SendConnectionAsync(connectionId, methodName, args, cancellationToken);

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName,
            object?[] args, CancellationToken cancellationToken = default) =>
            _inner.SendConnectionsAsync(connectionIds, methodName, args, cancellationToken);

        public override Task SendGroupAsync(string groupName, string methodName, object?[] args,
            CancellationToken cancellationToken = default) =>
            _inner.SendGroupAsync(groupName, methodName, args, cancellationToken);

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[] args,
            CancellationToken cancellationToken = default) =>
            _inner.SendGroupsAsync(groupNames, methodName, args, cancellationToken);

        public override Task SendGroupExceptAsync(string groupName, string methodName, object?[] args,
            IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default) =>
            _inner.SendGroupExceptAsync(groupName, methodName, args, excludedConnectionIds, cancellationToken);

        public override Task SendUserAsync(string userId, string methodName, object?[] args,
            CancellationToken cancellationToken = default) =>
            _inner.SendUserAsync(userId, methodName, args, cancellationToken);

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[] args,
            CancellationToken cancellationToken = default) =>
            _inner.SendUsersAsync(userIds, methodName, args, cancellationToken);

        public override Task RemoveFromGroupAsync(string connectionId, string groupName,
            CancellationToken cancellationToken = default) =>
            _inner.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
}
}
