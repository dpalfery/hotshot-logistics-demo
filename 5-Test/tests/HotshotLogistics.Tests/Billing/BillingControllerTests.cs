// <copyright file="BillingControllerTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Api.Controllers;
using HotshotLogistics.Api.Models;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
    ///     Integration tests for the BillingController.
    /// </summary>
    public class BillingControllerTests
    {
        private readonly BillingController _controller;
        private readonly Mock<IBillingService> _mockBillingService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BillingControllerTests" /> class.
        /// </summary>
        public BillingControllerTests()
        {
            _mockBillingService = new Mock<IBillingService>();
            Mock<IPaymentProcessorFactory> mockPaymentProcessorFactory = new();
            Mock<ILogger<BillingController>> mockLogger = new();
            _controller = new BillingController(_mockBillingService.Object, mockPaymentProcessorFactory.Object,
                mockLogger.Object);
        }

        /// <summary>
        ///     Tests that GenerateInvoice creates and returns an invoice.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GenerateInvoice_WithValidJobId_CreatesAndReturnsInvoice()
        {
            // Arrange
            string jobId = "test-job-id";
            Invoice expectedInvoice = CreateTestInvoice("invoice-1", "customer-1");

            _mockBillingService.Setup(s => s.GenerateInvoiceAsync(jobId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedInvoice);

            // Act
            ActionResult<Invoice> result = await _controller.GenerateInvoice(jobId);

            // Assert
            result.Should().NotBeNull();
            CreatedAtActionResult createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            Invoice returnedInvoice = createdResult.Value.Should().BeAssignableTo<Invoice>().Subject;
            returnedInvoice.Id.Should().Be("invoice-1");
        }

        /// <summary>
        ///     Tests that GenerateInvoice returns BadRequest for invalid job ID.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GenerateInvoice_WithInvalidJobId_ReturnsBadRequest()
        {
            // Arrange
            string jobId = "invalid-job-id";

            _mockBillingService.Setup(s => s.GenerateInvoiceAsync(jobId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Invalid job ID"));

            // Act
            ActionResult<Invoice> result = await _controller.GenerateInvoice(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that GenerateInvoice returns NotFound when job doesn't exist.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GenerateInvoice_WhenJobNotFound_ReturnsNotFound()
        {
            // Arrange
            string jobId = "non-existent-job";

            _mockBillingService.Setup(s => s.GenerateInvoiceAsync(jobId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("Job not found"));

            // Act
            ActionResult<Invoice> result = await _controller.GenerateInvoice(jobId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        /// <summary>
        ///     Tests that GetCustomerInvoices returns invoices for the customer.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetCustomerInvoices_ReturnsCustomerInvoices()
        {
            // Arrange
            string customerId = "customer-1";
            List<Invoice> expectedInvoices =
            [
                CreateTestInvoice("invoice-1", customerId),
                CreateTestInvoice("invoice-2", customerId)
            ];

            _mockBillingService.Setup(s => s.GetCustomerInvoicesAsync(customerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedInvoices);

            // Act
            ActionResult<IEnumerable<Invoice>> result = await _controller.GetCustomerInvoices(customerId);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            List<Invoice> returnedInvoices =
                okResult.Value.Should().BeAssignableTo<IEnumerable<Invoice>>().Subject.ToList();
            returnedInvoices.Should().HaveCount(2);
            returnedInvoices.All(i => i.CustomerId == customerId).Should().BeTrue();
        }

        /// <summary>
        ///     Tests that GetOverdueInvoices returns overdue invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetOverdueInvoices_ReturnsOverdueInvoices()
        {
            // Arrange
            List<Invoice> overdueInvoices =
            [
                CreateTestInvoice("overdue-1", "customer-1", InvoiceStatus.Overdue),
                CreateTestInvoice("overdue-2", "customer-2", InvoiceStatus.Overdue)
            ];

            _mockBillingService.Setup(s => s.GetOverdueInvoicesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(overdueInvoices);

            // Act
            ActionResult<IEnumerable<Invoice>> result = await _controller.GetOverdueInvoices();

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            List<Invoice> returnedInvoices =
                okResult.Value.Should().BeAssignableTo<IEnumerable<Invoice>>().Subject.ToList();
            returnedInvoices.Should().HaveCount(2);
            returnedInvoices.All(i => i.Status == InvoiceStatus.Overdue).Should().BeTrue();
        }

        /// <summary>
        ///     Tests that ProcessPayment processes payment successfully.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithValidRequest_ProcessesPaymentSuccessfully()
        {
            // Arrange
            string invoiceId = "invoice-1";
            ProcessPaymentRequest request = new()
            {
                Amount = 1000m,
                PaymentMethod = "Credit Card",
                Reference = "REF123"
            };

            _mockBillingService.Setup(s =>
                    s.ProcessPaymentAsync(invoiceId, request.Amount, request.PaymentMethod,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            ActionResult<PaymentResult> result = await _controller.ProcessPayment(invoiceId, request);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            PaymentResult paymentResult = okResult.Value.Should().BeOfType<PaymentResult>().Subject;
            paymentResult.Success.Should().BeTrue();
            paymentResult.InvoiceId.Should().Be(invoiceId);
            paymentResult.Amount.Should().Be(request.Amount);
        }

        /// <summary>
        ///     Tests that ProcessPayment returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            string invoiceId = "invoice-1";

            // Act
            ActionResult<PaymentResult> result = await _controller.ProcessPayment(invoiceId, null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that ProcessPayment returns BadRequest for zero amount.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithZeroAmount_ReturnsBadRequest()
        {
            // Arrange
            string invoiceId = "invoice-1";
            ProcessPaymentRequest request = new()
            {
                Amount = 0m,
                PaymentMethod = "Credit Card"
            };

            // Act
            ActionResult<PaymentResult> result = await _controller.ProcessPayment(invoiceId, request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that ProcessPayment returns BadRequest for empty payment method.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithEmptyPaymentMethod_ReturnsBadRequest()
        {
            // Arrange
            string invoiceId = "invoice-1";
            ProcessPaymentRequest request = new()
            {
                Amount = 1000m,
                PaymentMethod = ""
            };

            // Act
            ActionResult<PaymentResult> result = await _controller.ProcessPayment(invoiceId, request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that CalculateTax calculates tax correctly.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CalculateTax_WithValidRequest_CalculatesTaxCorrectly()
        {
            // Arrange
            TaxCalculationRequest request = new()
            {
                Amount = 1000m,
                State = "CA"
            };
            decimal expectedTaxAmount = 87.5m; // 8.75% tax rate

            _mockBillingService.Setup(s =>
                    s.CalculateTaxAsync(request.Amount, request.State, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTaxAmount);

            // Act
            ActionResult<TaxCalculationResult> result = await _controller.CalculateTax(request);

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            TaxCalculationResult taxResult = okResult.Value.Should().BeOfType<TaxCalculationResult>().Subject;
            taxResult.Amount.Should().Be(request.Amount);
            taxResult.State.Should().Be(request.State);
            taxResult.TaxAmount.Should().Be(expectedTaxAmount);
            taxResult.TotalAmount.Should().Be(request.Amount + expectedTaxAmount);
        }

        /// <summary>
        ///     Tests that CalculateTax returns BadRequest for null request.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CalculateTax_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            ActionResult<TaxCalculationResult> result = await _controller.CalculateTax(null!);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that CalculateTax returns BadRequest for zero amount.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task CalculateTax_WithZeroAmount_ReturnsBadRequest()
        {
            // Arrange
            TaxCalculationRequest request = new()
            {
                Amount = 0m,
                State = "CA"
            };

            // Act
            ActionResult<TaxCalculationResult> result = await _controller.CalculateTax(request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        /// <summary>
        ///     Tests that GetAccountsReceivableReport returns report with overdue invoices.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task GetAccountsReceivableReport_ReturnsReportWithOverdueInvoices()
        {
            // Arrange
            List<Invoice> overdueInvoices =
            [
                CreateTestInvoice("overdue-1", "customer-1", InvoiceStatus.Overdue, 1000m, 500m),
                CreateTestInvoice("overdue-2", "customer-2", InvoiceStatus.Overdue, 2000m, 1500m)
            ];

            _mockBillingService.Setup(s => s.GetOverdueInvoicesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(overdueInvoices);

            // Act
            ActionResult<AccountsReceivableReport> result = await _controller.GetAccountsReceivableReport();

            // Assert
            result.Should().NotBeNull();
            OkObjectResult okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            AccountsReceivableReport report = okResult.Value.Should().BeOfType<AccountsReceivableReport>().Subject;
            report.TotalOverdueAmount.Should().Be(2000m); // 500 + 1500
            report.OverdueInvoiceCount.Should().Be(2);
            report.OverdueInvoices.Should().HaveCount(2);
        }

        /// <summary>
        ///     Creates a test invoice for testing purposes.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <param name="customerId">The customer ID.</param>
        /// <param name="status">The invoice status.</param>
        /// <param name="totalAmount">The total amount.</param>
        /// <param name="balanceDue">The balance due.</param>
        /// <returns>A test invoice instance.</returns>
        private static Invoice CreateTestInvoice(string id, string customerId,
            InvoiceStatus status = InvoiceStatus.Sent, decimal totalAmount = 1000m, decimal balanceDue = 1000m)
        {
            return new Invoice
            {
                Id = id,
                CustomerId = customerId,
                InvoiceNumber = $"INV-{id}",
                Status = status,
                TotalAmount = totalAmount,
                PaidAmount = totalAmount - balanceDue,
                DueDate = DateTime.UtcNow.AddDays(-30),
                CreatedAt = DateTime.UtcNow.AddDays(-35),
                LineItems = new List<InvoiceLineItem>()
            };
        }
    }
}
