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

        /// <summary>
        ///     Tests that every repository method that accepts a cancellation token passes it on to the factory.
        /// </summary>
        /// <param name="operation">The repository method under test.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Theory]
        [InlineData("Payment.GetByInvoiceIdAsync")]
        [InlineData("Payment.GetByTransactionIdAsync")]
        [InlineData("Payment.GetByStatusAsync")]
        [InlineData("Payment.UpdatePaymentStatusAsync")]
        [InlineData("Driver.GetDriversAsync")]
        [InlineData("Driver.GetDriverByIdAsync")]
        [InlineData("JobAssignment.GetActiveAssignmentsAsync")]
        [InlineData("JobAssignment.GetByDriverIdAsync")]
        [InlineData("Invoice.GetByIdAsync")]
        public async Task Operation_WithCancellationToken_PassesTokenToFactory(string operation)
        {
            using CancellationTokenSource cancellation = new();
            CancellationToken token = cancellation.Token;
            Mock<ISqlConnectionFactory> factory = new();
            factory.Setup(f => f.OpenConnectionAsync(token)).ThrowsAsync(new TimeoutException());
            ISqlConnectionFactory fake = factory.Object;

            Task call = operation switch
            {
                "Payment.GetByInvoiceIdAsync" => new PaymentRepository(fake).GetByInvoiceIdAsync("i", token),
                "Payment.GetByTransactionIdAsync" => new PaymentRepository(fake).GetByTransactionIdAsync("t", token),
                "Payment.GetByStatusAsync" => new PaymentRepository(fake).GetByStatusAsync(PaymentStatus.Pending, token),
                "Payment.UpdatePaymentStatusAsync" => new PaymentRepository(fake).UpdatePaymentStatusAsync(
                    "p", PaymentStatus.Pending, null, token),
                "Driver.GetDriversAsync" => new DriverRepository(fake).GetDriversAsync(token),
                "Driver.GetDriverByIdAsync" => new DriverRepository(fake).GetDriverByIdAsync(1, token),
                "JobAssignment.GetActiveAssignmentsAsync" => new JobAssignmentRepository(fake)
                    .GetActiveAssignmentsAsync(token),
                "JobAssignment.GetByDriverIdAsync" => new JobAssignmentRepository(fake).GetByDriverIdAsync(1, token),
                "Invoice.GetByIdAsync" => new InvoiceRepository(fake).GetByIdAsync("i", token),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };

            await Assert.ThrowsAsync<TimeoutException>(() => call);
            factory.Verify(f => f.OpenConnectionAsync(token), Times.Once);
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
