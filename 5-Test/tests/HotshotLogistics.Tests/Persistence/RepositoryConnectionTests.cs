// <copyright file="RepositoryConnectionTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using Microsoft.Data.SqlClient;
using Moq;

namespace HotshotLogistics.Tests.Persistence
{
    /// <summary>
    ///     Tests that repositories get every connection from <see cref="ISqlConnectionFactory" /> and dispose it,
    ///     using a fake factory so no database is needed.
    /// </summary>
    public class RepositoryConnectionTests
    {
        /// <summary>
        ///     Gets one operation per repository connection site: Job, Invoice, LocationTracking and the shared base.
        /// </summary>
        public static TheoryData<string> Operations =>
        [
            nameof(JobRepository),
            nameof(InvoiceRepository),
            nameof(LocationTrackingRepository),
            nameof(CustomerRepository)
        ];

        /// <summary>
        ///     Tests that a failure to open a connection reaches the caller unchanged.
        /// </summary>
        /// <param name="repository">The repository under test.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Theory]
        [MemberData(nameof(Operations))]
        public async Task Operation_WhenFactoryFails_ThrowsTheFactoryException(string repository)
        {
            TimeoutException failure = new("connection factory down");
            Mock<ISqlConnectionFactory> factory = new();
            factory.Setup(f => f.OpenConnectionAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);

            TimeoutException thrown = await Assert.ThrowsAsync<TimeoutException>(() => Run(repository, factory.Object));

            thrown.Should().BeSameAs(failure);
        }

        /// <summary>
        ///     Tests that each operation asks the factory for exactly one connection and disposes it when the
        ///     command fails.
        /// </summary>
        /// <param name="repository">The repository under test.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Theory]
        [MemberData(nameof(Operations))]
        public async Task Operation_UsesOneFactoryConnectionAndDisposesIt(string repository)
        {
            // An unopened connection: the command fails, which proves the repository disposes what it was given.
            SqlConnection connection = new();
            bool disposed = false;
            connection.Disposed += (_, _) => disposed = true;
            Mock<ISqlConnectionFactory> factory = new();
            factory.Setup(f => f.OpenConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection);

            await Assert.ThrowsAsync<InvalidOperationException>(() => Run(repository, factory.Object));

            factory.Verify(f => f.OpenConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
            disposed.Should().BeTrue();
        }

        /// <summary>
        ///     Tests that the caller's cancellation token reaches the factory.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task GetJobsByStatusAsync_PassesCancellationTokenToFactory()
        {
            using CancellationTokenSource cancellation = new();
            Mock<ISqlConnectionFactory> factory = new();
            factory.Setup(f => f.OpenConnectionAsync(cancellation.Token)).ThrowsAsync(new TimeoutException());
            JobRepository repository = new(factory.Object);

            await Assert.ThrowsAsync<TimeoutException>(() =>
                repository.GetJobsByStatusAsync(HotshotLogistics.Core.Enums.JobStatus.Pending, cancellation.Token));

            factory.Verify(f => f.OpenConnectionAsync(cancellation.Token), Times.Once);
        }

        private static Task Run(string repository, ISqlConnectionFactory factory)
        {
            return repository switch
            {
                nameof(JobRepository) => new JobRepository(factory).GetJobsByStatusAsync(
                    HotshotLogistics.Core.Enums.JobStatus.Pending),
                nameof(InvoiceRepository) => new InvoiceRepository(factory).GetAgingReportAsync(),
                nameof(LocationTrackingRepository) => new LocationTrackingRepository(factory).AddBatchAsync(
                    [new LocationTracking("test-job", 1, 0m, 0m)]),
                nameof(CustomerRepository) => new CustomerRepository(factory).GetAllAsync(),
                _ => throw new ArgumentOutOfRangeException(nameof(repository), repository, null)
            };
        }
    }
}
