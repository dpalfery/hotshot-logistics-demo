using HotshotLogistics.Application.Services;
using HotshotLogistics.Contracts.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Jobs;

public sealed class JobServiceTests
{
    private readonly Mock<IJobRepository> _repository = new();
    private JobService CreateService() => new(
        _repository.Object, Mock.Of<ICustomerRepository>(), Mock.Of<IDriverRepository>(),
        Mock.Of<INotificationService>(), Mock.Of<IMappingService>(), Mock.Of<ILogger<JobService>>());

    [Fact]
    public async Task GetJobStatusSummaryAsync_WithAllStatusesPresent_MapsEachCount()
    {
        _repository.Setup(r => r.GetJobStatusCountsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int>
            {
                [JobStatus.Pending] = 2, [JobStatus.Assigned] = 3,
                [JobStatus.EnRoute] = 4, [JobStatus.Received] = 5
            });
        Assert.Equal(new JobStatusSummaryDto(2, 3, 4, 5), await CreateService().GetJobStatusSummaryAsync());
    }

    [Fact]
    public async Task GetJobStatusSummaryAsync_WithMissingStatuses_ReturnsZeroForThem()
    {
        _repository.Setup(r => r.GetJobStatusCountsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int> { [JobStatus.Assigned] = 3 });
        Assert.Equal(new JobStatusSummaryDto(0, 3, 0, 0), await CreateService().GetJobStatusSummaryAsync());
    }

    [Fact]
    public async Task GetJobStatusSummaryAsync_WithNoJobs_ReturnsAllZero()
    {
        _repository.Setup(r => r.GetJobStatusCountsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int>());
        Assert.Equal(new JobStatusSummaryDto(0, 0, 0, 0), await CreateService().GetJobStatusSummaryAsync());
    }

    [Fact]
    public async Task GetJobStatusSummaryAsync_WithCustomerId_PassesTrimmedCustomerFilter()
    {
        _repository.Setup(r => r.GetJobStatusCountsAsync(It.IsAny<JobFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int>());
        await CreateService().GetJobStatusSummaryAsync("  cust-9  ");
        _repository.Verify(r => r.GetJobStatusCountsAsync(
            It.Is<JobFilterDto>(f => f.CustomerId == "cust-9"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetJobStatusSummaryAsync_WithNullOrWhitespaceCustomerId_PassesNullFilter(string? customerId)
    {
        _repository.Setup(r => r.GetJobStatusCountsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<JobStatus, int>());
        await CreateService().GetJobStatusSummaryAsync(customerId);
        _repository.Verify(r => r.GetJobStatusCountsAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobStatusSummaryAsync_WhenRepositoryThrows_PropagatesException()
    {
        InvalidOperationException error = new("failure");
        _repository.Setup(r => r.GetJobStatusCountsAsync(null, It.IsAny<CancellationToken>())).ThrowsAsync(error);
        Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().GetJobStatusSummaryAsync()));
    }
}
