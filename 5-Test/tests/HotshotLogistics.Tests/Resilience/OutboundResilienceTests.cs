// <copyright file="OutboundResilienceTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using FluentAssertions;
using HotshotLogistics.Data;
using HotshotLogistics.Data.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace HotshotLogistics.Tests.Resilience
{
    /// <summary>
    ///     Tests the retry, timeout and circuit-breaker pipeline that every outbound HTTP client gets,
    ///     over a scripted handler so no test touches the network or sleeps through a real backoff.
    /// </summary>
    public class OutboundResilienceTests
    {
        private const string s_clientName = "resilience-test";

        [Fact]
        public async Task SendAsync_WhenFirstAttemptIsTransientFailure_RetriesAndReturnsSuccess()
        {
            ScriptedHandler handler = new((call, _) => Task.FromResult(
                new HttpResponseMessage(call == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)));
            await using ServiceProvider provider = BuildProvider(handler);

            using HttpResponseMessage response = await CreateClient(provider)
                .GetAsync("http://example.invalid/", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            handler.Calls.Should().Be(2);
        }

        [Fact]
        public async Task SendAsync_WhenEveryAttemptFails_StopsAfterTheConfiguredRetries()
        {
            ScriptedHandler handler = new((_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)));
            await using ServiceProvider provider = BuildProvider(
                handler,
                ("Retry:MaxRetryAttempts", "2"),
                ("CircuitBreaker:MinimumThroughput", "100"));

            using HttpResponseMessage response = await CreateClient(provider)
                .GetAsync("http://example.invalid/", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            handler.Calls.Should().Be(3, "one attempt plus two retries");
        }

        [Fact]
        public async Task SendAsync_WhenServerReturnsNotFound_DoesNotRetry()
        {
            ScriptedHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
            await using ServiceProvider provider = BuildProvider(handler);

            using HttpResponseMessage response = await CreateClient(provider)
                .GetAsync("http://example.invalid/", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task SendAsync_WhenAttemptHangs_TimesOutInsteadOfWaitingForever()
        {
            ScriptedHandler handler = new(async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            await using ServiceProvider provider = BuildProvider(handler);

            Func<Task> act = () => CreateClient(provider)
                .GetAsync("http://example.invalid/", TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<TimeoutRejectedException>();
            handler.Calls.Should().Be(2, "the timed-out attempt is retried once");
        }

        [Fact]
        public async Task SendAsync_WhenCallerCancels_DoesNotRetry()
        {
            using CancellationTokenSource caller = new();
            ScriptedHandler handler = new(async (_, token) =>
            {
                await caller.CancelAsync();
                token.ThrowIfCancellationRequested();
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            await using ServiceProvider provider = BuildProvider(handler);

            Func<Task> act = () => CreateClient(provider).GetAsync("http://example.invalid/", caller.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            handler.Calls.Should().Be(1);
        }

        [Fact]
        public async Task SendAsync_AfterRepeatedFailures_OpensTheCircuitAndFailsFast()
        {
            ScriptedHandler handler = new((_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)));
            await using ServiceProvider provider = BuildProvider(handler);
            HttpClient client = CreateClient(provider);
            using HttpResponseMessage failed =
                await client.GetAsync("http://example.invalid/", TestContext.Current.CancellationToken);
            int callsBeforeOpen = handler.Calls;

            Func<Task> act = () => client.GetAsync("http://example.invalid/", TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<BrokenCircuitException>();
            handler.Calls.Should().Be(callsBeforeOpen, "an open circuit never reaches the handler");
        }

        [Fact]
        public void CreateClient_WhenAttemptTimeoutExceedsTotalTimeout_RejectsTheSettings()
        {
            ScriptedHandler handler = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
            using ServiceProvider provider = BuildProvider(
                handler,
                ("AttemptTimeout:Timeout", "00:00:20"),
                ("TotalRequestTimeout:Timeout", "00:00:05"));

            Func<HttpClient> act = () => CreateClient(provider);

            act.Should().Throw<OptionsValidationException>();
        }

        [Fact]
        public async Task AddMappingServices_RegistersTheResiliencePipelineOnTheMappingClient()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Resilience:Outbound:Retry:MaxRetryAttempts"] = "5",
                })
                .Build();
            ServiceCollection services = new();
            services.AddSingleton(configuration);
            services.AddLogging();
            services.AddMappingServices();
            await using ServiceProvider provider = services.BuildServiceProvider();

            HttpStandardResilienceOptions options = provider
                .GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
                .Get("MappingService-standard");

            options.Retry.MaxRetryAttempts.Should().Be(5, "the mapping client's pipeline binds Resilience:Outbound");
        }

        private static ServiceProvider BuildProvider(ScriptedHandler handler, params (string Key, string Value)[] overrides)
        {
            Dictionary<string, string?> settings = new()
            {
                ["Resilience:Outbound:Retry:MaxRetryAttempts"] = "1",
                ["Resilience:Outbound:Retry:Delay"] = "00:00:00.001",
                ["Resilience:Outbound:Retry:UseJitter"] = "false",
                ["Resilience:Outbound:Retry:BackoffType"] = "Constant",
                ["Resilience:Outbound:AttemptTimeout:Timeout"] = "00:00:00.200",
                ["Resilience:Outbound:TotalRequestTimeout:Timeout"] = "00:00:10",
                ["Resilience:Outbound:CircuitBreaker:SamplingDuration"] = "00:00:01",
                ["Resilience:Outbound:CircuitBreaker:MinimumThroughput"] = "2",
                ["Resilience:Outbound:CircuitBreaker:FailureRatio"] = "0.5",
                ["Resilience:Outbound:CircuitBreaker:BreakDuration"] = "00:00:30",
            };
            foreach ((string key, string value) in overrides)
            {
                settings[$"Resilience:Outbound:{key}"] = value;
            }

            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            ServiceCollection services = new();
            services.AddSingleton(configuration);
            services.AddHttpClient(s_clientName)
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddOutboundResilience();
            return services.BuildServiceProvider();
        }

        private static HttpClient CreateClient(ServiceProvider provider)
        {
            return provider.GetRequiredService<IHttpClientFactory>().CreateClient(s_clientName);
        }

        private sealed class ScriptedHandler : HttpMessageHandler
        {
            private readonly Func<int, CancellationToken, Task<HttpResponseMessage>> _respond;
            private int _calls;

            public ScriptedHandler(Func<int, CancellationToken, Task<HttpResponseMessage>> respond)
            {
                _respond = respond;
            }

            public int Calls => Volatile.Read(ref _calls);

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return _respond(Interlocked.Increment(ref _calls), cancellationToken);
            }
        }
    }
}
