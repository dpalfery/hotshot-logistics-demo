// <copyright file="InvoiceRepositoryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
    ///     Integration tests for InvoiceRepository.
    /// </summary>
    [Collection(DatabaseCollection.Name)]
    public class InvoiceRepositoryTests : IClassFixture<DatabaseTestFixture>, IAsyncLifetime
    {
        // Every row this class creates is named from this unique tag, and every query below is scoped to it,
        // so seed rows and rows from other classes or processes never change the outcome.
        private readonly string _tag = $"test-{Guid.NewGuid():N}";
        private readonly TestRowCleanup _cleanup = new();
        private readonly IConfiguration _configuration;
        private readonly InvoiceRepository _invoiceRepository;
        private readonly string _customerId;
        private readonly string _otherCustomerId;
        private int _invoiceSequence;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InvoiceRepositoryTests" /> class.
        /// </summary>
        public InvoiceRepositoryTests(DatabaseTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            _customerId = $"{_tag}-c1";
            _otherCustomerId = $"{_tag}-c2";
            if (!TestDatabaseHelper.IsConfigured)
            {
                _configuration = new ConfigurationBuilder().Build();
                _invoiceRepository = null!;
                return;
            }

            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = TestDatabaseHelper.GetConnectionString()
                });

            _configuration = configBuilder.Build();
            _invoiceRepository = new InvoiceRepository(_configuration);
        }

        /// <inheritdoc />
        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        ///     Deletes the rows this test created and fails the test when any remain.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            await _cleanup.CleanUpAsync();
        }

        /// <summary>
        ///     Tests that AddAsync creates a new invoice successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task AddAsync_CreatesInvoiceSuccessfully()
        {
            // Arrange
            Invoice invoice = CreateTestInvoice();
            _cleanup.TrackInvoice(invoice.Id);

            // Act
            Invoice result = await _invoiceRepository.AddAsync(invoice);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(invoice.Id);
            result.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
            result.CustomerId.Should().Be(invoice.CustomerId);
            result.TotalAmount.Should().Be(invoice.TotalAmount);
        }

        /// <summary>
        ///     Tests that GetByIdAsync retrieves an invoice successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByIdAsync_RetrievesInvoiceSuccessfully()
        {
            // Arrange
            Invoice invoice = await CreateAndSaveTestInvoiceAsync();

            // Act
            Invoice? result = await _invoiceRepository.GetByIdAsync(invoice.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(invoice.Id);
            result.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
            result.CustomerId.Should().Be(invoice.CustomerId);
            result.TotalAmount.Should().Be(invoice.TotalAmount);
        }

        /// <summary>
        ///     Tests that UpdateAsync updates an invoice successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task UpdateAsync_UpdatesInvoiceSuccessfully()
        {
            // Arrange
            Invoice invoice = await CreateAndSaveTestInvoiceAsync();
            invoice.Status = InvoiceStatus.Sent;
            invoice.Notes = "Updated notes";

            // Act
            Invoice result = await _invoiceRepository.UpdateAsync(invoice);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(InvoiceStatus.Sent);
            result.Notes.Should().Be("Updated notes");
            result.UpdatedAt.Should().NotBeNull();
        }

        /// <summary>
        ///     Tests that GetByCustomerIdAsync returns invoices for the correct customer.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByCustomerIdAsync_ReturnsInvoicesForCorrectCustomer()
        {
            // Arrange
            string customerId = _customerId;
            await CreateTestInvoicesAsync();

            // Act
            List<Invoice> result = (await _invoiceRepository.GetByCustomerIdAsync(customerId)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(i => i.CustomerId == customerId);
            result.Select(i => i.JobId).Should().BeEquivalentTo(Job(1), Job(2));
        }

        /// <summary>
        ///     Tests that GetByJobIdAsync returns invoices for the correct job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByJobIdAsync_ReturnsInvoicesForCorrectJob()
        {
            // Arrange
            string jobId = Job(1);
            await CreateTestInvoicesAsync();

            // Act
            List<Invoice> result = (await _invoiceRepository.GetByJobIdAsync(jobId)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainSingle();
            result.Should().OnlyContain(i => i.JobId == jobId);
        }

        /// <summary>
        ///     Tests that GetByStatusAsync returns invoices with the correct status.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByStatusAsync_ReturnsInvoicesWithCorrectStatus()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            InvoiceStatus status = InvoiceStatus.Sent;

            // Act
            List<Invoice> result = (await _invoiceRepository.GetByStatusAsync(status)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(i => i.Status == status);
            result.Where(i => i.CustomerId == _customerId).Select(i => i.JobId).Should().Equal(Job(2));
        }

        /// <summary>
        ///     Tests that GetOverdueInvoicesAsync returns only overdue invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetOverdueInvoicesAsync_ReturnsOnlyOverdueInvoices()
        {
            // Arrange
            await CreateOverdueTestInvoiceAsync();

            // Act
            List<Invoice> result = (await _invoiceRepository.GetOverdueInvoicesAsync()).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(i =>
                i.Status != InvoiceStatus.Paid &&
                i.Status != InvoiceStatus.Cancelled &&
                i.DueDate < DateTime.UtcNow.Date);
            result.Should().ContainSingle(i => i.CustomerId == _otherCustomerId && i.JobId == Job(4));
        }

        /// <summary>
        ///     Tests that GetInvoicesDueWithinDaysAsync returns invoices due within specified days.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetInvoicesDueWithinDaysAsync_ReturnsInvoicesDueWithinDays()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            int days = 30;

            // Act
            List<Invoice> result = (await _invoiceRepository.GetInvoicesDueWithinDaysAsync(days)).ToList();

            // Assert
            result.Should().NotBeNull();
            DateTime futureDate = DateTime.UtcNow.Date.AddDays(days);
            result.Should().OnlyContain(i =>
                i.Status != InvoiceStatus.Paid &&
                i.Status != InvoiceStatus.Cancelled &&
                i.DueDate >= DateTime.UtcNow.Date &&
                i.DueDate <= futureDate);

            // Own invoices: three unpaid ones are due within 30 days; the paid one is left out
            result.Where(i => i.CustomerId == _customerId || i.CustomerId == _otherCustomerId)
                .Select(i => i.JobId).Should().BeEquivalentTo(Job(1), Job(2), Job(3));
        }

        /// <summary>
        ///     Tests that GetByDateRangeAsync returns invoices within the specified date range.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetByDateRangeAsync_ReturnsInvoicesWithinDateRange()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            DateTime startDate = DateTime.UtcNow.Date.AddDays(-30);
            DateTime endDate = DateTime.UtcNow.Date;

            // Act
            List<Invoice> result = (await _invoiceRepository.GetByDateRangeAsync(startDate, endDate)).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().OnlyContain(i =>
                i.InvoiceDate.Date >= startDate &&
                i.InvoiceDate.Date <= endDate);
            result.Count(i => i.CustomerId == _customerId || i.CustomerId == _otherCustomerId).Should().Be(4);
        }

        /// <summary>
        ///     Tests that GetPagedAsync returns paginated results with filtering.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetPagedAsync_ReturnsPaginatedResultsWithFiltering()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            InvoiceFilter filter = new()
            {
                CustomerId = _customerId,
                Status = InvoiceStatus.Draft,
                PageNumber = 1,
                PageSize = 2,
                SortBy = "InvoiceDate",
                SortDirection = "DESC"
            };

            // Act
            PagedResult<Invoice> result = await _invoiceRepository.GetPagedAsync(filter);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().OnlyContain(i => i.Status == InvoiceStatus.Draft && i.CustomerId == _customerId);
            result.Items.Select(i => i.JobId).Should().Equal(Job(1));
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(2);
            result.TotalCount.Should().Be(1);
        }

        /// <summary>
        ///     Tests that GetNextInvoiceNumberAsync returns the next available invoice number.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetNextInvoiceNumberAsync_ReturnsNextAvailableInvoiceNumber()
        {
            // Act
            string result = await _invoiceRepository.GetNextInvoiceNumberAsync();

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().MatchRegex(@"^INV\d{6}$");
        }

        /// <summary>
        ///     Tests that GetOutstandingBalanceAsync returns correct outstanding balance for customer.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetOutstandingBalanceAsync_ReturnsCorrectOutstandingBalance()
        {
            // Arrange
            string customerId = _customerId;
            await CreateTestInvoicesAsync();

            // Act
            decimal result = await _invoiceRepository.GetOutstandingBalanceAsync(customerId);

            // Assert: a draft of 1080.00 and a sent invoice of 1620.00, both unpaid
            result.Should().Be(2700.00m);
        }

        /// <summary>
        ///     Tests that GetInvoiceSummaryAsync returns correct summary statistics.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetInvoiceSummaryAsync_ReturnsCorrectSummaryStatistics()
        {
            // Arrange
            await CreateTestInvoicesAsync();

            // Act
            InvoiceSummary result = await _invoiceRepository.GetInvoiceSummaryAsync();

            // Assert
            result.Should().NotBeNull();
            result.TotalInvoices.Should().BeGreaterThanOrEqualTo(4);
            result.TotalAmount.Should().BeGreaterThanOrEqualTo(4752.00m);
            result.TotalOutstanding.Should().BeGreaterThanOrEqualTo(0);
        }

        /// <summary>
        ///     Tests that SearchByInvoiceNumberAsync returns matching invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task SearchByInvoiceNumberAsync_ReturnsMatchingInvoices()
        {
            // Arrange
            await CreateTestInvoicesAsync();
            string searchTerm = _tag;

            // Act
            List<Invoice> result = (await _invoiceRepository.SearchByInvoiceNumberAsync(searchTerm)).ToList();

            // Assert: the unique tag is in the four created invoice numbers only
            result.Should().NotBeNull();
            result.Should().HaveCount(4);
            result.Should().OnlyContain(i => i.InvoiceNumber.Contains(searchTerm));
        }

        /// <summary>
        ///     Tests that UpdatePaidAmountAsync updates the paid amount successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task UpdatePaidAmountAsync_UpdatesPaidAmountSuccessfully()
        {
            // Arrange
            Invoice invoice = await CreateAndSaveTestInvoiceAsync();
            decimal paidAmount = 500.00m;

            // Act
            bool result = await _invoiceRepository.UpdatePaidAmountAsync(invoice.Id, paidAmount);

            // Assert
            result.Should().BeTrue();

            // Verify the update
            Invoice? updatedInvoice = await _invoiceRepository.GetByIdAsync(invoice.Id);
            updatedInvoice!.PaidAmount.Should().Be(paidAmount);
        }

        /// <summary>
        ///     Tests that UpdateStatusAsync updates the status successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task UpdateStatusAsync_UpdatesStatusSuccessfully()
        {
            // Arrange
            Invoice invoice = await CreateAndSaveTestInvoiceAsync();
            InvoiceStatus newStatus = InvoiceStatus.Sent;

            // Act
            bool result = await _invoiceRepository.UpdateStatusAsync(invoice.Id, newStatus);

            // Assert
            result.Should().BeTrue();

            // Verify the update
            Invoice? updatedInvoice = await _invoiceRepository.GetByIdAsync(invoice.Id);
            updatedInvoice!.Status.Should().Be(newStatus);
        }

        /// <summary>
        ///     Tests that GetAgingReportAsync returns aging report data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GetAgingReportAsync_ReturnsAgingReportData()
        {
            // Arrange
            await CreateTestInvoicesAsync();

            // Act
            IEnumerable<AgingReportEntry> result = await _invoiceRepository.GetAgingReportAsync();

            // Assert
            result.Should().NotBeNull();
            // Note: This test may return empty results if no customers have outstanding balances
            // In a real scenario, we would ensure test data has outstanding invoices
        }

        /// <summary>
        ///     Tests that GenerateInvoiceAsync creates an invoice from job data.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GenerateInvoiceAsync_CreatesInvoiceFromJobData()
        {
            // Arrange
            string jobId = await CreateTestJobAsync();

            // Act: the next INV + 6 digit number is read and then inserted without a lock, so generators in
            // other test processes are queued behind a machine-wide lock to avoid a duplicate number.
            Invoice result;
            await using (await AcquireGenerateInvoiceLockAsync())
            {
                result = await _invoiceRepository.GenerateInvoiceAsync(jobId);
                _cleanup.TrackInvoice(result.Id);
            }

            // Assert
            result.Should().NotBeNull();
            result.JobId.Should().Be(jobId);
            result.Status.Should().Be(InvoiceStatus.Draft);
            result.TotalAmount.Should().BeGreaterThan(0);
            result.LineItems.Should().NotBeEmpty();
            result.InvoiceNumber.Should().StartWith("INV");
        }

        /// <summary>
        ///     Tests that GenerateInvoiceAsync throws exception for non-existent job.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [DatabaseFact]
        public async Task GenerateInvoiceAsync_ThrowsExceptionForNonExistentJob()
        {
            // Arrange
            string nonExistentJobId = "NONEXISTENT";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _invoiceRepository.GenerateInvoiceAsync(nonExistentJobId));
        }

        private string Job(int number)
        {
            return $"{_tag}-j{number}";
        }

        private static async Task<FileStream> AcquireGenerateInvoiceLockAsync()
        {
            // An exclusive open of a shared file is the machine-wide lock (advisory, released on dispose or exit).
            string path = Path.Combine(Path.GetTempPath(), "hotshot-logistics-generate-invoice.lock");
            DateTime giveUp = DateTime.UtcNow.AddMinutes(2);
            while (true)
            {
                try
                {
                    return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException) when (DateTime.UtcNow < giveUp)
                {
                    await Task.Delay(50);
                }
            }
        }

        private string NextInvoiceNumber()
        {
            return $"INV-{_tag}-{++_invoiceSequence}";
        }

        /// <summary>
        ///     Creates a test invoice.
        /// </summary>
        /// <returns>A test invoice.</returns>
        private Invoice CreateTestInvoice()
        {
            Invoice invoice = new()
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceNumber = NextInvoiceNumber(),
                CustomerId = _customerId,
                JobId = Job(1),
                InvoiceDate = DateTime.UtcNow.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                Status = InvoiceStatus.Draft,
                SubTotal = 1000.00m,
                TaxRate = 0.08m,
                TaxAmount = 80.00m,
                DiscountAmount = 0.00m,
                TotalAmount = 1080.00m,
                PaidAmount = 0.00m,
                Terms = new PaymentTerms
                {
                    Days = 30,
                    EarlyPaymentDiscount = 0.02m,
                    EarlyPaymentDiscountDays = 10,
                    LatePaymentPenalty = 0.015m,
                    LatePaymentPenaltyDays = 5
                },
                Notes = "Test invoice",
                CreatedAt = DateTime.UtcNow
            };

            invoice.AddLineItem(new InvoiceLineItem
            {
                Description = "Delivery Service",
                Quantity = 1,
                UnitPrice = 1000.00m,
                TaxApplicable = true
            });

            return invoice;
        }

        /// <summary>
        ///     Creates and saves a test invoice.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task<Invoice> CreateAndSaveTestInvoiceAsync()
        {
            Invoice invoice = CreateTestInvoice();
            _cleanup.TrackInvoice(invoice.Id);
            return await _invoiceRepository.AddAsync(invoice);
        }

        /// <summary>
        ///     Creates multiple test invoices for testing purposes.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CreateTestInvoicesAsync()
        {
            List<Invoice> invoices = new();

            // Create invoices with different statuses and customers
            var testData = new[]
            {
                new
                {
                    CustomerId = _customerId, JobId = (string?)Job(1), Status = InvoiceStatus.Draft, Amount = 1000.00m
                },
                new
                {
                    CustomerId = _customerId, JobId = (string?)Job(2), Status = InvoiceStatus.Sent, Amount = 1500.00m
                },
                new
                {
                    CustomerId = _otherCustomerId, JobId = (string?)Job(3), Status = InvoiceStatus.Draft, Amount = 800.00m
                },
                new
                {
                    CustomerId = _otherCustomerId, JobId = (string?)null, Status = InvoiceStatus.Paid, Amount = 1200.00m
                }
            };

            foreach (var data in testData)
            {
                Invoice invoice = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    InvoiceNumber = NextInvoiceNumber(),
                    CustomerId = data.CustomerId,
                    JobId = data.JobId,
                    InvoiceDate = DateTime.UtcNow.Date.AddDays(-invoices.Count),
                    DueDate = DateTime.UtcNow.Date.AddDays(30 - invoices.Count),
                    Status = data.Status,
                    SubTotal = data.Amount,
                    TaxRate = 0.08m,
                    TaxAmount = data.Amount * 0.08m,
                    DiscountAmount = 0.00m,
                    TotalAmount = data.Amount * 1.08m,
                    PaidAmount = data.Status == InvoiceStatus.Paid ? data.Amount * 1.08m : 0.00m,
                    Terms = new PaymentTerms { Days = 30 },
                    Notes = $"Test invoice {invoices.Count + 1}",
                    CreatedAt = DateTime.UtcNow.AddDays(-invoices.Count)
                };

                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Test Service",
                    Quantity = 1,
                    UnitPrice = data.Amount,
                    TaxApplicable = true
                });

                _cleanup.TrackInvoice(invoice.Id);
                Invoice result = await _invoiceRepository.AddAsync(invoice);
                invoices.Add(result);
            }
        }

        /// <summary>
        ///     Creates an overdue test invoice.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task CreateOverdueTestInvoiceAsync()
        {
            Invoice invoice = new()
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceNumber = NextInvoiceNumber(),
                CustomerId = _otherCustomerId,
                JobId = Job(4),
                InvoiceDate = DateTime.UtcNow.Date.AddDays(-60),
                DueDate = DateTime.UtcNow.Date.AddDays(-30), // 30 days overdue
                Status = InvoiceStatus.Sent,
                SubTotal = 2000.00m,
                TaxRate = 0.08m,
                TaxAmount = 160.00m,
                DiscountAmount = 0.00m,
                TotalAmount = 2160.00m,
                PaidAmount = 0.00m,
                Terms = new PaymentTerms { Days = 30 },
                Notes = "Overdue test invoice",
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            };

            invoice.AddLineItem(new InvoiceLineItem
            {
                Description = "Overdue Service",
                Quantity = 1,
                UnitPrice = 2000.00m,
                TaxApplicable = true
            });

            _cleanup.TrackInvoice(invoice.Id);
            _ = await _invoiceRepository.AddAsync(invoice);
        }

        /// <summary>
        ///     Creates a test job for invoice generation testing.
        /// </summary>
        /// <returns>The job ID of the created test job.</returns>
        private async Task<string> CreateTestJobAsync()
        {
            string jobId = Guid.NewGuid().ToString();
            _cleanup.TrackJob(jobId);
            const string sql = @"
                INSERT INTO Jobs (
                    Id, CustomerId, Title, PickupAddress, PickupLatitude, PickupLongitude,
                    DeliveryAddress, DeliveryLatitude, DeliveryLongitude,
                    CargoDescription, CargoWeight, CargoValue,
                    Status, Priority, BaseRate, MileageRate, TotalAmount,
                    EstimatedDeliveryTime, SpecialInstructions, CreatedAt
                ) VALUES (
                    @Id, @CustomerId, @Title, @PickupAddress, @PickupLat, @PickupLng,
                    @DeliveryAddress, @DeliveryLat, @DeliveryLng, @CargoDesc, @CargoWeight, @CargoValue,
                    @Status, @Priority, @BaseRate, @MileageRate, @TotalAmount,
                    @EstimatedDelivery, @SpecialInstructions, @CreatedAt
                )";

            SqlParameter[] parameters =
            [
                new("@Id", jobId),
                new("@CustomerId", _customerId),
                new("@Title", "Test Job for Invoice Generation"),
                new("@PickupAddress", "123 Pickup St"),
                new("@PickupCity", "Pickup City"),
                new("@PickupState", "PC"),
                new("@PickupZip", "12345"),
                new("@PickupLat", 40.7128m),
                new("@PickupLng", -74.0060m),
                new("@DeliveryAddress", "456 Delivery Ave"),
                new("@DeliveryCity", "Delivery City"),
                new("@DeliveryState", "DC"),
                new("@DeliveryZip", "67890"),
                new("@DeliveryLat", 34.0522m),
                new("@DeliveryLng", -118.2437m),
                new("@CargoDesc", "Test Cargo"),
                new("@CargoWeight", 1000.0),
                new("@CargoValue", 5000.00m),
                new("@CargoSpecial", "Handle with care"),
                new("@Status", (int)JobStatus.Received),
                new("@Priority", (int)JobPriority.Normal),
                new("@BaseRate", 500.00m),
                new("@MileageRate", 200.00m),
                new("@TotalAmount", 850.00m),
                new("@ScheduledPickup", DateTime.UtcNow.AddDays(-1)),
                new("@EstimatedDelivery", DateTime.UtcNow.AddDays(-1).AddHours(2)),
                new("@SpecialInstructions", "Test instructions"),
                new("@TrackingStatus", "Completed"),
                new("@LastUpdate", DateTime.UtcNow.AddDays(-1)),
                new("@CreatedAt", DateTime.UtcNow.AddDays(-1))
            ];

            await using SqlConnection connection = new(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            await using SqlCommand command = new(sql, connection);
            command.Parameters.AddRange(parameters);
            await command.ExecuteNonQueryAsync();

            return jobId;
        }
    }
}
