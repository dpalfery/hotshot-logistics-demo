// <copyright file="PaymentRetryTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
    ///     Tests which payment-processing failures <see cref="BillingService" /> retries. A payment retry
    ///     is not idempotent by itself, so only failures that cannot have charged the customer are retried.
    /// </summary>
    public class PaymentRetryTests
    {
        private const string s_invoiceId = "invoice-retry-1";
        private const decimal s_amount = 100m;

        private readonly Mock<IInvoiceRepository> _invoices = new();
        private readonly Mock<IPaymentRepository> _payments = new();
        private readonly Mock<IPaymentProcessor> _processor = new();
        private readonly List<Dictionary<string, string>> _seenMetadata = [];
        private readonly BillingService _service;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PaymentRetryTests" /> class.
        /// </summary>
        public PaymentRetryTests()
        {
            _invoices.Setup(r => r.GetByIdAsync(s_invoiceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Invoice
                {
                    Id = s_invoiceId,
                    CustomerId = "customer-retry-1",
                    TotalAmount = s_amount,
                    InvoiceNumber = "INV-RETRY-1"
                });
            _invoices.Setup(r => r.UpdatePaidAmountAsync(s_invoiceId, s_amount)).ReturnsAsync(true);
            Mock<IPaymentProcessorFactory> factory = new();
            factory.Setup(f => f.GetProcessorForPaymentMethod(It.IsAny<PaymentMethodType>()))
                .Returns(_processor.Object);
            _service = new BillingService(
                _invoices.Object,
                Mock.Of<IJobRepository>(),
                Mock.Of<ICustomerRepository>(),
                _payments.Object,
                Mock.Of<INotificationService>(),
                factory.Object,
                NullLogger<BillingService>.Instance,
                Options.Create(new PaymentRetryOptions { BaseDelay = TimeSpan.FromMilliseconds(1) }));
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenProcessorDeclines_DoesNotRetryAndMarksPaymentFailed()
        {
            SetupProcessor(_ => Task.FromResult(Failure("CARD_DECLINED")));

            bool result = await _service.ProcessPaymentAsync(s_invoiceId, s_amount, "Credit Card", TestContext.Current.CancellationToken);

            result.Should().BeFalse();
            VerifyCalls(1);
            _payments.Verify(p => p.UpdateAsync(It.Is<Payment>(x => x.Status == PaymentStatus.Failed)), Times.Once);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenProcessorReportsTemporaryError_RetriesUntilItSucceeds()
        {
            SetupProcessor(call => Task.FromResult(call < 3 ? Failure("TEMPORARY_ERROR") : Success()));

            bool result = await _service.ProcessPaymentAsync(s_invoiceId, s_amount, "Credit Card", TestContext.Current.CancellationToken);

            result.Should().BeTrue();
            VerifyCalls(3);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenTransportFailsTwice_RetriesAndSucceeds()
        {
            SetupProcessor(call => call < 3
                ? Task.FromException<PaymentProcessingResult>(new HttpRequestException("connection reset"))
                : Task.FromResult(Success()));

            bool result = await _service.ProcessPaymentAsync(s_invoiceId, s_amount, "Credit Card", TestContext.Current.CancellationToken);

            result.Should().BeTrue();
            VerifyCalls(3);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenTransientFailureNeverClears_StopsAfterTheConfiguredRetries()
        {
            SetupProcessor(_ => Task.FromResult(Failure("TEMPORARY_ERROR")));

            bool result = await _service.ProcessPaymentAsync(s_invoiceId, s_amount, "Credit Card", TestContext.Current.CancellationToken);

            result.Should().BeFalse();
            VerifyCalls(4, "one attempt plus three retries");
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenProcessorThrowsUnexpectedly_DoesNotRetry()
        {
            SetupProcessor(_ => Task.FromException<PaymentProcessingResult>(new InvalidOperationException("bug")));

            bool result = await _service.ProcessPaymentAsync(s_invoiceId, s_amount, "Credit Card", TestContext.Current.CancellationToken);

            result.Should().BeFalse();
            VerifyCalls(1);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenCallerCancels_StopsWithoutRetryingOrSwallowingIt()
        {
            using CancellationTokenSource caller = new();
            SetupProcessor(_ =>
            {
                caller.Cancel();
                return Task.FromException<PaymentProcessingResult>(new OperationCanceledException(caller.Token));
            });

            Func<Task<bool>> act = () => _service.ProcessPaymentAsync(s_invoiceId, s_amount, "Credit Card", caller.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
            VerifyCalls(1);
        }

        [Fact]
        public async Task ProcessPaymentAsync_WhenRetrying_SendsTheSameIdempotencyKeyEveryAttempt()
        {
            SetupProcessor(call => Task.FromResult(call < 3 ? Failure("TEMPORARY_ERROR") : Success()));

            await _service.ProcessPaymentAsync(s_invoiceId, s_amount, "Credit Card", TestContext.Current.CancellationToken);

            _seenMetadata.Should().HaveCount(3);
            _seenMetadata.Select(m => m["idempotency_key"]).Distinct().Should().ContainSingle();
        }

        private static PaymentProcessingResult Failure(string errorCode)
        {
            return new PaymentProcessingResult { Success = false, Message = "failed", ErrorCode = errorCode };
        }

        private static PaymentProcessingResult Success()
        {
            return new PaymentProcessingResult { Success = true, TransactionId = "txn-1", Message = "ok" };
        }

        private void SetupProcessor(Func<int, Task<PaymentProcessingResult>> respond)
        {
            int calls = 0;
            _processor.Setup(p => p.ProcessPaymentAsync(
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<PaymentMethodDetails>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((decimal _, string _, PaymentMethodDetails _, Dictionary<string, string> metadata, CancellationToken _) =>
                {
                    _seenMetadata.Add(metadata);
                    return respond(Interlocked.Increment(ref calls));
                });
        }

        private void VerifyCalls(int expected, string because = "")
        {
            _processor.Verify(
                p => p.ProcessPaymentAsync(
                    It.IsAny<decimal>(),
                    It.IsAny<string>(),
                    It.IsAny<PaymentMethodDetails>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(expected),
                because);
        }
    }
}
