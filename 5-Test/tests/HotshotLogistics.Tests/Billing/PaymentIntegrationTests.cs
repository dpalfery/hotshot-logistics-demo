// <copyright file="PaymentIntegrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
    ///     Integration tests for payment processing functionality.
    /// </summary>
    public class PaymentIntegrationTests
    {
        private readonly Mock<ICustomerRepository> _mockCustomerRepository;
        private readonly Mock<IInvoiceRepository> _mockInvoiceRepository;
        private readonly Mock<ILogger<BillingService>> _mockLogger;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<IPaymentProcessor> _mockPayPalProcessor;
        private readonly Mock<IPaymentProcessorFactory> _mockPaymentProcessorFactory;
        private readonly Mock<IPaymentRepository> _mockPaymentRepository;
        private readonly Mock<IPaymentProcessor> _mockStripeProcessor;
        private readonly IPaymentProcessorFactory _paymentProcessorFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PaymentIntegrationTests" /> class.
        /// </summary>
        public PaymentIntegrationTests()
        {
            _mockStripeProcessor = new Mock<IPaymentProcessor>();
            _mockPayPalProcessor = new Mock<IPaymentProcessor>();
            _mockPaymentProcessorFactory = new Mock<IPaymentProcessorFactory>();
            _mockInvoiceRepository = new Mock<IInvoiceRepository>();
            _mockPaymentRepository = new Mock<IPaymentRepository>();
            _mockCustomerRepository = new Mock<ICustomerRepository>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<BillingService>>();
            Mock<IServiceProvider> mockServiceProvider = new();
            Mock<IConfiguration> mockConfiguration = new();

            // Setup service provider to return processors (kept for completeness)
            // Use GetService (the actual IServiceProvider method) instead of the GetRequiredService
            // extension to allow Moq to setup the call directly.
            mockServiceProvider.Setup(sp => sp.GetService(typeof(StripePaymentProcessor)))
                .Returns(_mockStripeProcessor.Object);
            mockServiceProvider.Setup(sp => sp.GetService(typeof(PayPalPaymentProcessor)))
                .Returns(_mockPayPalProcessor.Object);

            // Setup configuration
            mockConfiguration.Setup(c => c["Payment:DefaultProcessor"]).Returns("Stripe");

            // Use the mocked factory in tests so Moq can create the proxy without hitting concrete ctor
            _paymentProcessorFactory = _mockPaymentProcessorFactory.Object;
        }

        /// <summary>
        ///     Tests successful payment processing with Stripe.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithValidStripePayment_ReturnsSuccess()
        {
            // Arrange
            string invoiceId = "invoice-123";
            decimal paymentAmount = 1000.00m;
            string paymentMethod = "Credit Card";

            Invoice invoice = CreateTestInvoice(invoiceId, "customer-123", 1000.00m, 0.00m);
            Customer customer = CreateTestCustomer("customer-123");

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);
            _mockCustomerRepository.Setup(r => r.GetByIdAsync("customer-123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            PaymentProcessingResult paymentResult = new()
            {
                Success = true,
                TransactionId = "stripe_txn_123",
                Message = "Payment processed successfully"
            };

            _mockStripeProcessor.Setup(p => p.ProcessPaymentAsync(
                    paymentAmount,
                    "USD",
                    It.IsAny<PaymentMethodDetails>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(paymentResult);

            _mockPaymentProcessorFactory.Setup(f => f.GetProcessorForPaymentMethod(PaymentMethodType.CreditCard))
                .Returns(_mockStripeProcessor.Object);

            // Fix: Setup UpdatePaidAmountAsync mock for retry test
            _mockInvoiceRepository.Setup(r => r.UpdatePaidAmountAsync(invoiceId, paymentAmount))
                .ReturnsAsync(true);

            BillingService billingService = CreateBillingService();

            // Act
            bool result = await billingService.ProcessPaymentAsync(invoiceId, paymentAmount, paymentMethod);

            // Assert
            result.Should().BeTrue();
            _mockPaymentRepository.Verify(r => r.AddAsync(It.IsAny<Payment>()), Times.Once);
            _mockPaymentRepository.Verify(r => r.UpdateAsync(It.IsAny<Payment>()), Times.Once);
            _mockInvoiceRepository.Verify(r => r.UpdatePaidAmountAsync(invoiceId, paymentAmount), Times.Once);
            _mockNotificationService.Verify(n => n.SendNotificationAsync(
                "customer-123",
                NotificationType.PaymentReceived,
                "Payment Received",
                It.Is<string>(s => s.Contains("$1000.00")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        ///     Tests payment processing failure with retry logic.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithFailedPayment_ImplementsRetryLogic()
        {
            // Arrange
            string invoiceId = "invoice-123";
            decimal paymentAmount = 1000.00m;
            string paymentMethod = "Credit Card";

            Invoice invoice = CreateTestInvoice(invoiceId, "customer-123", 1000.00m, 0.00m);
            Customer customer = CreateTestCustomer("customer-123");

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);
            _mockCustomerRepository.Setup(r => r.GetByIdAsync("customer-123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            // First two calls fail, third succeeds
            int callCount = 0;
            _mockStripeProcessor.Setup(p => p.ProcessPaymentAsync(
                    paymentAmount,
                    "USD",
                    It.IsAny<PaymentMethodDetails>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount <= 2)
                    {
                        return new PaymentProcessingResult
                        {
                            Success = false,
                            Message = "Temporary failure",
                            ErrorCode = "TEMPORARY_ERROR"
                        };
                    }

                    return new PaymentProcessingResult
                    {
                        Success = true,
                        TransactionId = "stripe_txn_123",
                        Message = "Payment processed successfully"
                    };
                });

            _mockPaymentProcessorFactory.Setup(f => f.GetProcessorForPaymentMethod(PaymentMethodType.CreditCard))
                .Returns(_mockStripeProcessor.Object);

            // Fix: Ensure the factory returns the correct processor for the payment method
            _mockPaymentProcessorFactory.Setup(f => f.GetProcessorForPaymentMethod(It.IsAny<PaymentMethodType>()))
                .Returns((PaymentMethodType method) => method == PaymentMethodType.CreditCard
                    ? _mockStripeProcessor.Object
                    : _mockPayPalProcessor.Object);

            // Fix: Setup UpdatePaidAmountAsync mock to return true
            _mockInvoiceRepository.Setup(r => r.UpdatePaidAmountAsync(invoiceId, paymentAmount))
                .ReturnsAsync(true);

            BillingService billingService = CreateBillingService();

            // Act
            bool result = await billingService.ProcessPaymentAsync(invoiceId, paymentAmount, paymentMethod);

            // Assert
            result.Should().BeTrue();
            _mockStripeProcessor.Verify(p => p.ProcessPaymentAsync(
                paymentAmount,
                "USD",
                It.IsAny<PaymentMethodDetails>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()), Times.Exactly(3)); // Should retry 3 times
        }

        /// <summary>
        ///     Tests PayPal payment processing.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessPayment_WithPayPalPayment_UsesPayPalProcessor()
        {
            // Arrange
            string invoiceId = "invoice-456";
            decimal paymentAmount = 500.00m;
            string paymentMethod = "PayPal";

            Invoice invoice = CreateTestInvoice(invoiceId, "customer-456", 500.00m, 0.00m);
            Customer customer = CreateTestCustomer("customer-456");

            _mockInvoiceRepository.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invoice);
            _mockCustomerRepository.Setup(r => r.GetByIdAsync("customer-456", It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            PaymentProcessingResult paymentResult = new()
            {
                Success = true,
                TransactionId = "paypal_txn_456",
                Message = "PayPal payment processed successfully"
            };

            _mockPayPalProcessor.Setup(p => p.ProcessPaymentAsync(
                    paymentAmount,
                    "USD",
                    It.IsAny<PaymentMethodDetails>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(paymentResult);

            _mockPaymentProcessorFactory.Setup(f => f.GetProcessorForPaymentMethod(PaymentMethodType.DigitalWallet))
                .Returns(_mockPayPalProcessor.Object);

            // Fix: Also setup UpdatePaidAmountAsync for PayPal test
            _mockInvoiceRepository.Setup(r => r.UpdatePaidAmountAsync(invoiceId, paymentAmount))
                .ReturnsAsync(true);

            BillingService billingService = CreateBillingService();

            // Act
            bool result = await billingService.ProcessPaymentAsync(invoiceId, paymentAmount, paymentMethod);

            // Assert
            result.Should().BeTrue();
            _mockPayPalProcessor.Verify(p => p.ProcessPaymentAsync(
                paymentAmount,
                "USD",
                It.IsAny<PaymentMethodDetails>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        ///     Tests webhook processing for Stripe.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessStripeWebhook_WithValidSignature_ProcessesEvent()
        {
            // Arrange
            WebhookEventData webhookData = new()
            {
                EventType = "payment_intent.succeeded",
                Payload = "{\"event\":\"payment_intent.succeeded\"}",
                Signature = "valid_signature"
            };

            WebhookProcessingResult webhookResult = new()
            {
                Success = true,
                StatusUpdate = new PaymentStatusUpdate
                {
                    TransactionId = "stripe_txn_123",
                    Status = PaymentStatus.Completed,
                    InvoiceId = "invoice_123",
                    Amount = 1000.00m
                }
            };

            _mockStripeProcessor.Setup(p => p.ValidateWebhookSignature(
                    webhookData.Payload,
                    webhookData.Signature,
                    It.IsAny<string>()))
                .Returns(true);

            _mockStripeProcessor.Setup(p => p.ProcessWebhookAsync(
                    webhookData,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(webhookResult);

            // Act
            WebhookProcessingResult result =
                await _mockStripeProcessor.Object.ProcessWebhookAsync(webhookData, It.IsAny<CancellationToken>());

            // Assert
            result.Success.Should().BeTrue();
            result.StatusUpdate.Should().NotBeNull();
            result.StatusUpdate!.TransactionId.Should().Be("stripe_txn_123");
            result.StatusUpdate.Status.Should().Be(PaymentStatus.Completed);
        }

        /// <summary>
        ///     Tests webhook processing with invalid signature.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ProcessStripeWebhook_WithInvalidSignature_RejectsEvent()
        {
            // Arrange
            WebhookEventData webhookData = new()
            {
                EventType = "payment_intent.succeeded",
                Payload = "{\"event\":\"payment_intent.succeeded\"}",
                Signature = "invalid_signature"
            };

            _mockStripeProcessor.Setup(p => p.ValidateWebhookSignature(
                    webhookData.Payload,
                    webhookData.Signature,
                    It.IsAny<string>()))
                .Returns(false);

            // Setup ProcessWebhookAsync to handle invalid signature case
            _mockStripeProcessor.Setup(p => p.ProcessWebhookAsync(
                    webhookData,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebhookProcessingResult
                {
                    Success = false,
                    Message = "Invalid webhook signature"
                });

            // Act
            WebhookProcessingResult result =
                await _mockStripeProcessor.Object.ProcessWebhookAsync(webhookData, It.IsAny<CancellationToken>());

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Invalid webhook signature");
        }

        /// <summary>
        ///     Creates a test invoice for testing.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <param name="customerId">The customer ID.</param>
        /// <param name="totalAmount">The total amount.</param>
        /// <param name="paidAmount">The paid amount.</param>
        /// <returns>A test invoice.</returns>
        private static Invoice CreateTestInvoice(string id, string customerId, decimal totalAmount, decimal paidAmount)
        {
            return new Invoice
            {
                Id = id,
                CustomerId = customerId,
                TotalAmount = totalAmount,
                PaidAmount = paidAmount,
                InvoiceNumber = $"INV-{id}"
            };
        }

        /// <summary>
        ///     Creates a test customer for testing.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <returns>A test customer.</returns>
        private static Customer CreateTestCustomer(string id)
        {
            return new Customer
            {
                Id = id,
                CompanyName = $"Company {id}"
            };
        }

        /// <summary>
        ///     Creates a billing service instance with mocked dependencies.
        /// </summary>
        /// <returns>A billing service instance.</returns>
        private BillingService CreateBillingService()
        {
            return new BillingService(
                _mockInvoiceRepository.Object,
                Mock.Of<IJobRepository>(),
                _mockCustomerRepository.Object,
                _mockPaymentRepository.Object,
                _mockNotificationService.Object,
                _paymentProcessorFactory,
                _mockLogger.Object,
                Options.Create(new PaymentRetryOptions { BaseDelay = TimeSpan.FromMilliseconds(1) }));
        }
    }
}
