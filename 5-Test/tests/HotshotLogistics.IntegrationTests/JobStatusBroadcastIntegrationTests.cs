using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using HotshotLogistics.Application.Hubs;
using HotshotLogistics.Contracts.Hubs;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotshotLogistics.IntegrationTests
{
    /// <summary>
    ///     Every API call that changes a job's status sends <c>JobStatusUpdated</c> to the admin dashboard group
    ///     once, through the real controller, service and database. Only the final SignalR send is faked.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class JobStatusBroadcastIntegrationTests : IntegrationTestBase
    {
        private const string s_jobStatusUpdated = "JobStatusUpdated";

        private readonly HttpClient _client;
        private readonly RecordingSignalRClient _signalR = new();

        public JobStatusBroadcastIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
            _client = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped<ISignalRClientWrapper>(_ => _signalR))).CreateClient();
            _client.DefaultRequestHeaders.Authorization = new("Test");
        }

        [Fact]
        public async Task CreateJob_SendsThePendingStatusToTheAdminsGroupOnce()
        {
            Job job = await CreateJobAsync("bc-create");

            VerifyAdminSendOnce(job.Id, JobStatus.Pending);
        }

        [Fact]
        public async Task UpdateJob_ThatChangesTheStatus_SendsTheNewStatusToTheAdminsGroupOnce()
        {
            Job job = await CreateJobAsync("bc-update");
            _signalR.Clear();
            Job changes = NewJob(job.Id, JobStatus.EnRoute);

            using HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/Job/{job.Id}", changes);

            response.EnsureSuccessStatusCode();
            VerifyAdminSendOnce(job.Id, JobStatus.EnRoute);
        }

        [Fact]
        public async Task AssignDriver_SendsAssignedToTheAdminsGroupOnce()
        {
            Job job = await CreateJobAsync("bc-assign");
            int driverId = await CreateDriverAsync();
            _signalR.Clear();

            using HttpResponseMessage response = await _client.PostAsJsonAsync(
                $"/api/Job/{job.Id}/assign-driver", new { driverId });

            response.EnsureSuccessStatusCode();
            VerifyAdminSendOnce(job.Id, JobStatus.Assigned);
        }

        [Theory]
        [InlineData(JobStatus.EnRoute)]
        [InlineData(JobStatus.Received)]
        public async Task UpdateJobStatus_SendsTheNewStatusToTheAdminsGroupOnce(JobStatus status)
        {
            Job job = await CreateJobAsync("bc-status");
            _signalR.Clear();

            using HttpResponseMessage response = await PutStatusAsync(job.Id, status);

            response.EnsureSuccessStatusCode();
            VerifyAdminSendOnce(job.Id, status);
        }

        [Fact]
        public async Task UpdateJobStatus_ForAJobThatDoesNotExist_SendsNothing()
        {
            using HttpResponseMessage response =
                await PutStatusAsync($"test-missing-{Guid.NewGuid():N}", JobStatus.EnRoute);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(_signalR.GroupSends);
        }

        private async Task<HttpResponseMessage> PutStatusAsync(string jobId, JobStatus status)
        {
            // Changing a status needs the manager or driver role; the default test identity is an admin
            using HttpRequestMessage request = new(HttpMethod.Put, $"/api/Job/{jobId}/status")
            {
                Content = JsonContent.Create(new { status })
            };
            request.Headers.Add("X-Test-Role", "Manager");
            return await _client.SendAsync(request);
        }

        private void VerifyAdminSendOnce(string jobId, JobStatus status)
        {
            GroupSend send = Assert.Single(_signalR.GroupSends, g => g.Group == RealtimeGroups.Admins);
            Assert.Equal(s_jobStatusUpdated, send.Method);
            Assert.Equal(new object[] { jobId, status }, send.Args);
        }

        private async Task<int> CreateDriverAsync()
        {
            string email = $"test.broadcast.{Guid.NewGuid():N}@test.com";
            Cleanup.TrackDriverEmail(email);
            using HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Drivers", new DriverDto
            {
                FirstName = "Broadcast",
                LastName = "Driver",
                Email = email,
                PhoneNumber = "(555) 123-4569",
                LicenseNumber = $"DRV-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                LicenseExpiryDate = DateTime.UtcNow.AddYears(3),
                IsActive = true
            });
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Driver creation failed with {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            }

            DriverDto? driver = await response.Content.ReadFromJsonAsync<DriverDto>();
            Assert.NotNull(driver);
            Cleanup.TrackDriver(driver.Id);
            return driver.Id;
        }

        private async Task<Job> CreateJobAsync(string idPrefix)
        {
            string id = $"test-{idPrefix}-{Guid.NewGuid():N}";
            Cleanup.TrackJob(id);
            Job job = NewJob(id, JobStatus.Pending);

            using HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Job", job);
            response.EnsureSuccessStatusCode();
            Job? created = await response.Content.ReadFromJsonAsync<Job>();
            Assert.NotNull(created);
            return created;
        }

        private static Job NewJob(string id, JobStatus status)
        {
            DateTime pickupTime = DateTime.UtcNow.AddHours(2);
            return new Job
            {
                Id = id,
                CustomerId = "cust-001", // Seeded customer, only referenced
                Title = "Own status broadcast job",
                PickupLocation = new Location
                {
                    Address = "1 Own Pickup St",
                    City = "OwnCity",
                    State = "CA",
                    PostalCode = "94043",
                    Country = "USA",
                    Latitude = 37.4220m,
                    Longitude = -122.0841m
                },
                DeliveryLocation = new Location
                {
                    Address = "2 Own Delivery Ave",
                    City = "OwnCity",
                    State = "CA",
                    PostalCode = "94041",
                    Country = "USA",
                    Latitude = 37.3861m,
                    Longitude = -122.0839m
                },
                Amount = 150m,
                Cargo = new CargoDetails
                {
                    Description = "Own test cargo",
                    Weight = 25,
                    IsHazardous = false,
                    Quantity = 1,
                    Value = 100
                },
                Status = status,
                Priority = JobPriority.Medium,
                Pricing = new PricingDetails
                { BaseRate = 100, MileageRate = 1.5m, TotalAmount = 150, Currency = "USD" },
                ScheduledPickupTime = pickupTime,
                EstimatedDeliveryTime = pickupTime.AddHours(8),
                SpecialInstructions = "Created by an integration test"
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>
    ///     One message sent to a SignalR group.
    /// </summary>
    internal sealed record GroupSend(string Group, string Method, object[] Args);

    /// <summary>
    ///     Records the SignalR sends instead of delivering them.
    /// </summary>
    internal sealed class RecordingSignalRClient : ISignalRClientWrapper
    {
        private readonly ConcurrentQueue<GroupSend> _groupSends = new();

        public IReadOnlyCollection<GroupSend> GroupSends => [.. _groupSends];

        public void Clear()
        {
            _groupSends.Clear();
        }

        public Task SendToGroupAsync(string groupName, string methodName, params object[] args)
        {
            _groupSends.Enqueue(new GroupSend(groupName, methodName, args));
            return Task.CompletedTask;
        }

        public Task SendToUserAsync(string userId, string methodName, params object[] args)
        {
            return Task.CompletedTask;
        }

        public Task SendToAllAsync(string methodName, params object[] args)
        {
            return Task.CompletedTask;
        }

        public Task SendToCallerAsync(string methodName, params object[] args)
        {
            return Task.CompletedTask;
        }
    }
}
