// <copyright file="RepositoryContractTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using Moq;

namespace HotshotLogistics.Tests.Persistence
{
    /// <summary>
    ///     Tests the generic repository contract, <see cref="IRepository{T}" />, and who adopts it.
    /// </summary>
    public class RepositoryContractTests
    {
        /// <summary>
        ///     Gets each repository implementation with the entity type its generic contract covers.
        /// </summary>
        public static TheoryData<Type, Type> Implementations =>
        [
            (typeof(CustomerRepository), typeof(Customer)),
            (typeof(DriverRepository), typeof(Driver)),
            (typeof(InvoiceRepository), typeof(Invoice)),
            (typeof(JobAssignmentRepository), typeof(JobAssignmentDto)),
            (typeof(JobRepository), typeof(Job)),
            (typeof(LocationTrackingRepository), typeof(LocationTracking)),
            (typeof(PaymentRepository), typeof(Payment))
        ];

        /// <summary>
        ///     Gets each repository contract with the entity type its generic contract covers.
        /// </summary>
        public static TheoryData<Type, Type> Contracts =>
        [
            (typeof(ICustomerRepository), typeof(Customer)),
            (typeof(IDriverRepository), typeof(Driver)),
            (typeof(IInvoiceRepository), typeof(Invoice)),
            (typeof(IJobAssignmentRepository), typeof(JobAssignmentDto)),
            (typeof(IJobRepository), typeof(Job)),
            (typeof(ILocationTrackingRepository), typeof(LocationTracking)),
            (typeof(IPaymentRepository), typeof(Payment))
        ];

        /// <summary>
        ///     Tests that every repository implementation provides the generic contract for its entity.
        /// </summary>
        /// <param name="repository">The repository implementation.</param>
        /// <param name="entity">The entity type.</param>
        [Theory]
        [MemberData(nameof(Implementations))]
        public void Repository_ImplementsGenericContractForItsEntity(Type repository, Type entity)
        {
            repository.Should().Implement(typeof(IRepository<>).MakeGenericType(entity));
        }

        /// <summary>
        ///     Tests that every repository contract extends the generic contract, so consumers can depend on it.
        /// </summary>
        /// <param name="contract">The repository contract.</param>
        /// <param name="entity">The entity type.</param>
        [Theory]
        [MemberData(nameof(Contracts))]
        public void RepositoryContract_ExtendsGenericContractForItsEntity(Type contract, Type entity)
        {
            contract.Should().Implement(typeof(IRepository<>).MakeGenericType(entity));
        }

        /// <summary>
        ///     Tests that every member of the generic contract takes a cancellation token, so callers can cancel any call.
        /// </summary>
        [Fact]
        public void GenericContract_EveryMemberTakesACancellationToken()
        {
            typeof(IRepository<>).GetMethods()
                .Where(method => method.GetParameters().All(p => p.ParameterType != typeof(CancellationToken)))
                .Select(method => method.Name)
                .Should().BeEmpty();
        }

        /// <summary>
        ///     Tests that a customer repository used through the generic contract reaches the same implementation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task CustomerRepository_ThroughGenericContract_UsesTheCustomerContractMock()
        {
            Mock<ICustomerRepository> mock = new();
            Customer customer = new() { Id = $"test-{Guid.NewGuid():N}" };
            mock.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);
            IRepository<Customer> generic = mock.Object;

            Customer? found = await generic.GetByIdAsync(customer.Id, TestContext.Current.CancellationToken);

            found.Should().BeSameAs(customer);
        }

        /// <summary>
        ///     Tests that adding a null customer is still refused with an <see cref="ArgumentException" />.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Fact]
        public async Task CustomerRepository_AddAsyncWithNull_ThrowsArgumentException()
        {
            IRepository<Customer> repository = new CustomerRepository(new Mock<ISqlConnectionFactory>().Object);

            await Assert.ThrowsAsync<ArgumentException>(() => repository.AddAsync(null!));
        }

        /// <summary>
        ///     Tests that the contracts project exposes one generic repository contract, not a second base interface.
        /// </summary>
        [Fact]
        public void Contracts_DoNotDeclareAnIBaseRepository()
        {
            typeof(IRepository<>).Assembly.GetTypes().Select(t => t.Name).Should().NotContain("IBaseRepository`1");
        }
    }
}
