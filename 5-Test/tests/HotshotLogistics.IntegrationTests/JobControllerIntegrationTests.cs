using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.IntegrationTests
{
    /// <summary>
    ///     Integration tests for the JobController.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class JobControllerIntegrationTests : IntegrationTestBase
    {
        public JobControllerIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
            // Set the test authentication header
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        [Fact]
        public async Task GetJobs_ReturnsSuccessAndListOfJobs()
        {
            // Act
            HttpResponseMessage response = await Client.GetAsync("/api/Job");

            // Assert
            response.EnsureSuccessStatusCode();
            PagedResult<Job>? pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Job>>();
            Assert.NotNull(pagedResult);
            Assert.NotNull(pagedResult.Items);
            Assert.NotEmpty(pagedResult.Items);
        }

        [Fact]
        public async Task GetJobs_WithPaging_ReturnsCorrectPage()
        {
            // Act
            HttpResponseMessage response = await Client.GetAsync("/api/Job?pageNumber=1&pageSize=10");

            // Assert
            response.EnsureSuccessStatusCode();
            PagedResult<Job>? pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<Job>>();
            Assert.NotNull(pagedResult);
            Assert.NotNull(pagedResult.Items);
            Assert.True(pagedResult.Items.Count() <= 10);
            Assert.Equal(1, pagedResult.PageNumber);
            Assert.Equal(10, pagedResult.PageSize);
        }


        [Fact]
        public async Task GetJob_WithValidId_ReturnsJob()
        {
            // Arrange
            Job ownJob = await CreateOwnJobAsync("get-job");
            string validJobId = ownJob.Id;

            // Act
            HttpResponseMessage response = await Client.GetAsync($"/api/Job/{validJobId}");

            // Assert
            response.EnsureSuccessStatusCode();
            Job? job = await response.Content.ReadFromJsonAsync<Job>();
            Assert.NotNull(job);
            Assert.Equal(validJobId, job.Id);
        }

        [Fact]
        public async Task GetJob_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            string invalidJobId = Guid.NewGuid().ToString();

            // Act
            HttpResponseMessage response = await Client.GetAsync($"/api/Job/{invalidJobId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateJob_WithValidData_ReturnsCreated()
        {
            // Arrange
            string uniqueId = $"test-job-{Guid.NewGuid():N}";
            Cleanup.TrackJob(uniqueId);
            DateTime baseTime = DateTime.UtcNow.AddMinutes(10); // Start 10 minutes from now to avoid timing issues
            DateTime pickupTime = new(baseTime.Year, baseTime.Month, baseTime.Day, baseTime.Hour, baseTime.Minute, 0,
                DateTimeKind.Utc);
            DateTime deliveryTime = pickupTime.AddHours(4); // Ensure 4-hour gap between pickup and delivery

            // Use Job with all required fields properly set
            Job newJob = new()
            {
                Id = uniqueId,
                CustomerId = "cust-001", // Seeded customer
                Title = "Urgent Tech Parts Delivery",
                PickupLocation = new Location
                {
                    Address = "100 Tech Park",
                    City = "Innovate",
                    State = "CA",
                    PostalCode = "94043",
                    Country = "USA",
                    Latitude = 37.4220m,
                    Longitude = -122.0841m
                },
                DeliveryLocation = new Location
                {
                    Address = "200 Consumer Ave",
                    City = "Market",
                    State = "CA",
                    PostalCode = "94041",
                    Country = "USA",
                    Latitude = 37.3861m,
                    Longitude = -122.0839m
                },
                Amount = 300m, // Required field
                Cargo = new CargoDetails
                {
                    Description = "Sensitive electronics",
                    Weight = 50,
                    IsHazardous = false,
                    Quantity = 1,
                    Value = 1000
                },
                Status = JobStatus.Pending,
                Priority = JobPriority.High,
                Pricing = new PricingDetails
                { BaseRate = 250, MileageRate = 1.75m, TotalAmount = 300, Currency = "USD" },
                ScheduledPickupTime = pickupTime,
                EstimatedDeliveryTime = deliveryTime,
                SpecialInstructions = "Handle with extreme care."
            };

            // Act
            HttpResponseMessage response = await Client.PostAsJsonAsync("/api/Job", newJob);

            // Debug: Check actual error response
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                HttpStatusCode statusCode = response.StatusCode;
                string? reasonPhrase = response.ReasonPhrase;

                // Log detailed error information
                Console.WriteLine($"Status Code: {statusCode}");
                Console.WriteLine($"Reason Phrase: {reasonPhrase}");
                Console.WriteLine($"Error Content: {errorContent}");

                // Try to get more details from headers
                foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
                {
                    Console.WriteLine($"Header {header.Key}: {string.Join(", ", header.Value)}");
                }

                foreach (KeyValuePair<string, IEnumerable<string>> contentHeader in response.Content.Headers)
                {
                    Console.WriteLine($"Content Header {contentHeader.Key}: {string.Join(", ", contentHeader.Value)}");
                }

                throw new Exception($"Job create failed with status {response.StatusCode}: {errorContent}");
            }

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode); // Should be Created for successful creation
            Job? createdJob = await response.Content.ReadFromJsonAsync<Job>();
            Assert.NotNull(createdJob);
            Assert.Equal(newJob.Title, createdJob.Title);
            Assert.Equal(uniqueId, createdJob.Id);
        }

        [Fact]
        public async Task UpdateJob_WithValidData_ReturnsNoContent()
        {
            // Arrange
            Job jobToUpdate = await CreateOwnJobAsync("update-job");

            // Create a simple Job with minimal valid data for update
            DateTime pickupTime = DateTime.UtcNow.AddHours(2);
            Job jobDto = new()
            {
                Id = jobToUpdate.Id,
                CustomerId = jobToUpdate.CustomerId,
                Title = "Updated Super Urgent Delivery", // Updated title
                PickupLocation = new Location
                {
                    Address = "Updated 100 Tech Park",
                    City = "Innovate",
                    State = "CA",
                    PostalCode = "94043",
                    Country = "USA",
                    Latitude = 37.4220m,
                    Longitude = -122.0841m
                },
                DeliveryLocation = new Location
                {
                    Address = "Updated 200 Consumer Ave",
                    City = "Market",
                    State = "CA",
                    PostalCode = "94041",
                    Country = "USA",
                    Latitude = 37.3861m,
                    Longitude = -122.0839m
                },
                Amount = 350m, // Updated amount
                Status = JobStatus.Assigned, // Updated status
                Priority = JobPriority.High,
                ScheduledPickupTime = pickupTime,
                // Use simple, reliable nested objects
                Cargo = new CargoDetails
                {
                    Description = "Updated sensitive electronics",
                    Weight = 60,
                    IsHazardous = false,
                    Quantity = 1,
                    Value = 1200
                },
                Pricing = new PricingDetails
                { BaseRate = 300, MileageRate = 2.0m, TotalAmount = 350, Currency = "USD" },
                SpecialInstructions = "Updated special handling instructions",
                EstimatedDeliveryTime = pickupTime.AddHours(4)
            };

            // Act
            HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/Job/{jobToUpdate.Id}", jobDto);

            // Debug: Check actual error response
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Job update failed with status {response.StatusCode}: {errorContent}");
            }

            // Assert
            Assert.Equal(HttpStatusCode.OK,
                response.StatusCode); // Changed from NoContent to OK since API returns the updated job

            // Verify update
            HttpResponseMessage getResponse = await Client.GetAsync($"/api/Job/{jobToUpdate.Id}");
            getResponse.EnsureSuccessStatusCode();
            Job? updatedJob = await getResponse.Content.ReadFromJsonAsync<Job>();
            Assert.NotNull(updatedJob);
            Assert.Equal("Updated Super Urgent Delivery", updatedJob.Title);
            Assert.Equal(JobStatus.Assigned, updatedJob.Status);
        }

        [Fact]
        public async Task DeleteJob_WithValidId_ReturnsNoContent()
        {
            // Arrange - First create a job to delete using Job
            string uniqueId = $"delete-job-{Guid.NewGuid():N}";
            Cleanup.TrackJob(uniqueId);
            DateTime pickupTime = DateTime.UtcNow.AddHours(2);
            Job testJob = new()
            {
                Id = uniqueId,
                CustomerId = "cust-001", // Seeded customer
                Title = "Job To Delete",
                PickupLocation = new Location
                {
                    Address = "123 Delete St",
                    City = "DeleteCity",
                    State = "DL",
                    PostalCode = "12345",
                    Country = "USA",
                    Latitude = 37.4220m,
                    Longitude = -122.0841m
                },
                DeliveryLocation = new Location
                {
                    Address = "456 Destination Ave",
                    City = "DestCity",
                    State = "DS",
                    PostalCode = "54321",
                    Country = "USA",
                    Latitude = 37.3861m,
                    Longitude = -122.0839m
                },
                Amount = 150m, // Required field
                Cargo = new CargoDetails
                {
                    Description = "Test cargo for deletion",
                    Weight = 25,
                    IsHazardous = false,
                    Quantity = 1,
                    Value = 100
                },
                Status = JobStatus.Pending,
                Priority = JobPriority.Medium,
                Pricing = new PricingDetails
                { BaseRate = 100, MileageRate = 1.5m, TotalAmount = 150, Currency = "USD" },
                ScheduledPickupTime = pickupTime,
                EstimatedDeliveryTime = pickupTime.AddHours(8), // Set EstimatedDeliveryTime 8 hours after pickup
                SpecialInstructions = "Job created for delete test"
            };

            // Create the job
            HttpResponseMessage createResponse = await Client.PostAsJsonAsync("/api/Job", testJob);

            // Debug: Check creation error
            if (!createResponse.IsSuccessStatusCode)
            {
                string createErrorContent = await createResponse.Content.ReadAsStringAsync();
                throw new Exception(
                    $"Job creation for delete test failed with status {createResponse.StatusCode}: {createErrorContent}");
            }

            createResponse.EnsureSuccessStatusCode();
            Job? createdJob = await createResponse.Content.ReadFromJsonAsync<Job>();
            Assert.NotNull(createdJob);
            string jobIdToDelete = createdJob.Id;

            // Act - Delete the job
            HttpResponseMessage response = await Client.DeleteAsync($"/api/Job/{jobIdToDelete}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // Verify it was deleted
            HttpResponseMessage getResponse = await Client.GetAsync($"/api/Job/{jobIdToDelete}");
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        }

        /// <summary>
        ///     Creates a job owned by the calling test through the API, so tests never read or change seed jobs
        ///     picked by position.
        /// </summary>
        /// <param name="idPrefix">Short label put in the job id.</param>
        /// <returns>The created job.</returns>
        private async Task<Job> CreateOwnJobAsync(string idPrefix)
        {
            string id = $"test-{idPrefix}-{Guid.NewGuid():N}";
            Cleanup.TrackJob(id);
            DateTime pickupTime = DateTime.UtcNow.AddHours(2);
            Job job = new()
            {
                Id = id,
                CustomerId = "cust-001", // Seeded customer, only referenced
                Title = $"Own job {idPrefix}",
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
                Status = JobStatus.Pending,
                Priority = JobPriority.Medium,
                Pricing = new PricingDetails
                { BaseRate = 100, MileageRate = 1.5m, TotalAmount = 150, Currency = "USD" },
                ScheduledPickupTime = pickupTime,
                EstimatedDeliveryTime = pickupTime.AddHours(8),
                SpecialInstructions = "Created by an integration test"
            };

            HttpResponseMessage response = await Client.PostAsJsonAsync("/api/Job", job);
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Job creation failed with status {response.StatusCode}: {errorContent}");
            }

            Job? created = await response.Content.ReadFromJsonAsync<Job>();
            Assert.NotNull(created);
            return created;
        }
    }
}
