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

        /// <summary>
        ///     Gets each repository paired with each member of the generic contract.
        /// </summary>
        public static TheoryData<string, string> GenericOperations()
        {
            TheoryData<string, string> data = [];
            string[] repositories =
            [
                nameof(CustomerRepository), nameof(DriverRepository), nameof(InvoiceRepository),
                nameof(JobAssignmentRepository), nameof(JobRepository), nameof(LocationTrackingRepository),
                nameof(PaymentRepository)
            ];
            string[] members = ["GetById", "GetAll", "Add", "Update", "Delete", "Exists"];
            foreach (string repository in repositories)
            {
                foreach (string member in members)
                {
                    data.Add(repository, member);
                }
            }

            return data;
        }

        /// <summary>
        ///     Tests that every member of the generic contract, on every repository, passes the caller's cancellation
        ///     token to the connection factory.
        /// </summary>
        /// <param name="repository">The repository under test.</param>
        /// <param name="member">The generic contract member under test.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Theory]
        [MemberData(nameof(GenericOperations))]
        public async Task GenericOperation_WithCancellationToken_PassesTokenToFactory(string repository, string member)
        {
            using CancellationTokenSource cancellation = new();
            CancellationToken token = cancellation.Token;
            Mock<ISqlConnectionFactory> factory = new();
            factory.Setup(f => f.OpenConnectionAsync(token)).ThrowsAsync(new TimeoutException());
            ISqlConnectionFactory fake = factory.Object;

            Task call = repository switch
            {
                nameof(CustomerRepository) => CallGeneric(new CustomerRepository(fake), member, new Customer(), token),
                nameof(DriverRepository) => CallGeneric(new DriverRepository(fake), member, new Driver(), token),
                nameof(InvoiceRepository) => CallGeneric(new InvoiceRepository(fake), member, new Invoice(), token),
                nameof(JobAssignmentRepository) => CallGeneric(
                    new JobAssignmentRepository(fake), member, new JobAssignmentDto(), token),
                nameof(JobRepository) => CallGeneric(new JobRepository(fake), member, new Job(), token),
                nameof(LocationTrackingRepository) => CallGeneric(
                    new LocationTrackingRepository(fake), member, new LocationTracking("test-job", 1, 0m, 0m), token),
                nameof(PaymentRepository) => CallGeneric(new PaymentRepository(fake), member, new Payment(), token),
                _ => throw new ArgumentOutOfRangeException(nameof(repository), repository, null)
            };

            await Assert.ThrowsAsync<TimeoutException>(() => call);
            factory.Verify(f => f.OpenConnectionAsync(token), Times.Once);
        }

        private static Task CallGeneric<T>(IRepository<T> repository, string member, T entity, CancellationToken token)
            where T : class
        {
            return member switch
            {
                "GetById" => repository.GetByIdAsync("test-id", token),
                "GetAll" => repository.GetAllAsync(token),
                "Add" => repository.AddAsync(entity, token),
                "Update" => repository.UpdateAsync(entity, token),
                "Delete" => repository.DeleteAsync("test-id", token),
                "Exists" => repository.ExistsAsync("test-id", token),
                _ => throw new ArgumentOutOfRangeException(nameof(member), member, null)
            };
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
