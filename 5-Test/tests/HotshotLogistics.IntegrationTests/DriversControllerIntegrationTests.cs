using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotshotLogistics.Domain.DTOs;

namespace HotshotLogistics.IntegrationTests
{
    // <copyright file="DriversControllerIntegrationTests.cs" company="PlaceholderCompany">
    // Copyright (c) PlaceholderCompany. All rights reserved.
    // </copyright>

    [Collection(DatabaseCollection.Name)]
    public class DriversControllerIntegrationTests : IntegrationTestBase
    {
        public DriversControllerIntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
            // Set the test authentication header
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        [Fact]
        public async Task GetDrivers_ReturnsSuccessAndListOfDrivers()
        {
            // Act
            HttpResponseMessage response = await Client.GetAsync("/api/Drivers");

            // Assert
            response.EnsureSuccessStatusCode();
            List<DriverDto>? drivers = await response.Content.ReadFromJsonAsync<List<DriverDto>>();
            drivers.Should().NotBeNull();
            drivers.Should().HaveCountGreaterThan(100); // Should have plenty of seed drivers
        }

        [Fact]
        public async Task GetDriver_WithValidId_ReturnsDriver()
        {
            // Arrange — seed IDs are identity-generated, so look up a known seeded driver first.
            const string seededEmail = "seed.driver001@local.test";
            HttpResponseMessage listResponse = await Client.GetAsync("/api/Drivers");
            listResponse.EnsureSuccessStatusCode();
            List<DriverDto>? drivers = await listResponse.Content.ReadFromJsonAsync<List<DriverDto>>();
            drivers.Should().NotBeNull();
            DriverDto expected = drivers.Should().ContainSingle(d => d.Email == seededEmail).Subject;

            // Act
            HttpResponseMessage response = await Client.GetAsync($"/api/Drivers/{expected.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            DriverDto? driver = await response.Content.ReadFromJsonAsync<DriverDto>();
            driver.Should().NotBeNull();
            driver.Id.Should().Be(expected.Id);
            driver.Email.Should().Be(seededEmail);
        }

        [Fact]
        public async Task GetDriver_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int invalidDriverId = int.MaxValue;

            // Act
            HttpResponseMessage response = await Client.GetAsync($"/api/Drivers/{invalidDriverId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateDriver_WithValidData_ReturnsCreated()
        {
            // Arrange
            string uniqueEmail = $"test.driver.{Guid.NewGuid():N}@test.com";
            Cleanup.TrackDriverEmail(uniqueEmail);
            DriverDto newDriver = new()
            {
                FirstName = "Test",
                LastName = "Driver",
                Email = uniqueEmail,
                PhoneNumber = "(555) 123-4567", // Valid US phone format
                LicenseNumber = "DRV123456", // Valid format: uppercase letters, numbers, hyphens
                LicenseExpiryDate =
                    DateTime.UtcNow
                        .AddYears(3), // Must be valid for at least 2 years (for age validation) and 6 months (for registration)
                IsActive = true
            };

            // Act
            HttpResponseMessage response = await Client.PostAsJsonAsync("/api/Drivers", newDriver);

            // Debug: Check actual error response if not successful
            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Driver creation failed with status {response.StatusCode}: {errorContent}");
            }

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            DriverDto? createdDriver = await response.Content.ReadFromJsonAsync<DriverDto>();
            createdDriver.Should().NotBeNull();
            Cleanup.TrackDriver(createdDriver.Id);
            createdDriver.Email.Should().Be(uniqueEmail);
            createdDriver.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpdateDriver_WithValidData_ReturnsOk()
        {
            // Arrange — create a driver to update so the test does not depend on seed identity values.
            string uniqueCreateEmail = $"update.create.{Guid.NewGuid():N}@test.com";
            string uniqueUpdateEmail = $"updated.driver.{Guid.NewGuid():N}@test.com";
            Cleanup.TrackDriverEmail(uniqueCreateEmail);
            Cleanup.TrackDriverEmail(uniqueUpdateEmail);
            HttpResponseMessage createResponse = await Client.PostAsJsonAsync("/api/Drivers", new DriverDto
            {
                FirstName = "Update",
                LastName = "Target",
                Email = uniqueCreateEmail,
                PhoneNumber = "(555) 123-4568",
                LicenseNumber = $"DRV-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                LicenseExpiryDate = DateTime.UtcNow.AddYears(3),
                IsActive = true
            });
            createResponse.EnsureSuccessStatusCode();
            DriverDto? createdDriver = await createResponse.Content.ReadFromJsonAsync<DriverDto>();
            createdDriver.Should().NotBeNull();
            Cleanup.TrackDriver(createdDriver.Id);

            DriverDto driverToUpdate = new()
            {
                Id = createdDriver.Id,
                FirstName = "Updated",
                LastName = "DriverTwo",
                Email = uniqueUpdateEmail,
                PhoneNumber = "(555) 987-6543",
                LicenseNumber = "DRV654321",
                LicenseExpiryDate = DateTime.UtcNow.AddYears(3),
                IsActive = false
            };

            // Act
            HttpResponseMessage response =
                await Client.PutAsJsonAsync($"/api/Drivers/{createdDriver.Id}", driverToUpdate);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Driver update failed with status {response.StatusCode}: {errorContent}");
            }

            // Assert
            DriverDto? updatedDriver = await response.Content.ReadFromJsonAsync<DriverDto>();

            updatedDriver.Should().NotBeNull();
            updatedDriver.FirstName.Should().Be("Updated");
            updatedDriver.IsActive.Should().BeFalse();
            updatedDriver.Email.Should().Be(uniqueUpdateEmail);
        }

        [Fact]
        public async Task DeleteDriver_WithValidId_ReturnsNoContent()
        {
            // Arrange - First create a driver to delete
            string uniqueDeleteEmail = $"delete.test.driver.{Guid.NewGuid():N}@example.com";
            Cleanup.TrackDriverEmail(uniqueDeleteEmail);
            DriverDto testDriver = new()
            {
                FirstName = "Delete",
                LastName = "TestDriver",
                Email = uniqueDeleteEmail,
                PhoneNumber = "(555) 123-9999", // Valid US phone format
                LicenseNumber = "DRV-DELETE", // Valid format: uppercase letters, numbers, hyphens
                LicenseExpiryDate = DateTime.UtcNow.AddYears(3), // Must meet validation requirements
                IsActive = true
            };

            // Create the driver
            HttpResponseMessage createResponse = await Client.PostAsJsonAsync("/api/Drivers", testDriver);
            createResponse.EnsureSuccessStatusCode();
            DriverDto? createdDriver = await createResponse.Content.ReadFromJsonAsync<DriverDto>();
            createdDriver.Should().NotBeNull();
            Cleanup.TrackDriver(createdDriver.Id);
            int driverIdToDelete = createdDriver.Id;

            // Act - Delete the driver
            HttpResponseMessage response = await Client.DeleteAsync($"/api/Drivers/{driverIdToDelete}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify it was soft-deleted (should still exist but IsActive = false)
            HttpResponseMessage getResponse = await Client.GetAsync($"/api/Drivers/{driverIdToDelete}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            DriverDto? deletedDriver = await getResponse.Content.ReadFromJsonAsync<DriverDto>();
            deletedDriver.Should().NotBeNull();
            deletedDriver.IsActive.Should().BeFalse(); // Should be soft-deleted (inactive)
        }
    }
}
