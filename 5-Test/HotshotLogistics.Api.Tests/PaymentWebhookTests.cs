using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentAssertions;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Contracts.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Proves the payment webhooks are reachable without a sign-in only because the provider's signature
    ///     is verified first: a request with a valid signature is processed; one with a missing, wrong, tampered
    ///     or stale signature is rejected and nothing is processed.
    /// </summary>
    public class PaymentWebhookTests : IDisposable
    {
        private const string s_stripePath = "/api/Billing/webhooks/stripe";
        private const string s_paypalPath = "/api/Billing/webhooks/paypal";

        private readonly RSA _paypalKey = RSA.Create(2048);
        private readonly X509Certificate2 _paypalCertificate;
        private readonly Mock<IBillingService> _billing = new();

        public PaymentWebhookTests()
        {
            _paypalCertificate = WebhookSigning.CreateCertificate(_paypalKey);
            _billing.Setup(b => b.GetCustomerInvoicesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
        }

        public void Dispose()
        {
            _paypalCertificate.Dispose();
            _paypalKey.Dispose();
        }

        [Fact]
        public async Task StripeWebhook_WithoutSignInButWithAValidSignature_IsProcessed()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory();
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await PostAsync(
                client,
                s_stripePath,
                WebhookSigning.StripeBody,
                StripeHeaders(WebhookSigning.StripeBody, WebhookSigning.StripeSecret, DateTimeOffset.UtcNow));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            VerifyProcessed(Times.Once());
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("garbage")]
        [InlineData("wrong-secret")]
        [InlineData("tampered-body")]
        [InlineData("stale")]
        [InlineData("future")]
        [InlineData("no-v1")]
        public async Task StripeWebhook_WithAnInvalidSignature_Returns401AndProcessesNothing(string scenario)
        {
            await using WebApplicationFactory<Program> factory = CreateFactory();
            using HttpClient client = factory.CreateClient();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Dictionary<string, string> headers = scenario switch
            {
                "missing" => [],
                "garbage" => new() { ["Stripe-Signature"] = "this-is-not-a-signature" },
                "wrong-secret" => StripeHeaders(WebhookSigning.StripeBody, "some-other-secret", now),
                "tampered-body" => StripeHeaders("{\"type\":\"payment_intent.payment_failed\"}", WebhookSigning.StripeSecret, now),
                "stale" => StripeHeaders(WebhookSigning.StripeBody, WebhookSigning.StripeSecret, now.AddMinutes(-10)),
                "future" => StripeHeaders(WebhookSigning.StripeBody, WebhookSigning.StripeSecret, now.AddMinutes(10)),
                _ => new() { ["Stripe-Signature"] = $"t={now.ToUnixTimeSeconds()}" },
            };

            using HttpResponseMessage response =
                await PostAsync(client, s_stripePath, WebhookSigning.StripeBody, headers);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            VerifyProcessed(Times.Never());
        }

        [Fact]
        public async Task PayPalWebhook_WithoutSignInButWithAValidSignature_IsProcessed()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory();
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await PostAsync(
                client,
                s_paypalPath,
                WebhookSigning.PayPalBody,
                WebhookSigning.PayPalHeaders(_paypalKey, DateTimeOffset.UtcNow));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            VerifyProcessed(Times.Once());
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("missing-cert-url")]
        [InlineData("wrong-key")]
        [InlineData("tampered-signature")]
        [InlineData("unsupported-algorithm")]
        [InlineData("stale")]
        [InlineData("future")]
        public async Task PayPalWebhook_WithAnInvalidSignature_Returns401AndProcessesNothing(string scenario)
        {
            await using WebApplicationFactory<Program> factory = CreateFactory();
            using HttpClient client = factory.CreateClient();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            using RSA otherKey = RSA.Create(2048);
            Dictionary<string, string> headers = scenario switch
            {
                "missing" => [],
                "missing-cert-url" => Without(WebhookSigning.PayPalHeaders(_paypalKey, now), "PAYPAL-CERT-URL"),
                "wrong-key" => WebhookSigning.PayPalHeaders(otherKey, now),
                "tampered-signature" => With(WebhookSigning.PayPalHeaders(_paypalKey, now), "PAYPAL-TRANSMISSION-SIG", "AAAA"),
                "unsupported-algorithm" => With(WebhookSigning.PayPalHeaders(_paypalKey, now), "PAYPAL-AUTH-ALGO", "SHA1withRSA"),
                "stale" => WebhookSigning.PayPalHeaders(_paypalKey, now.AddMinutes(-10)),
                _ => WebhookSigning.PayPalHeaders(_paypalKey, now.AddMinutes(10)),
            };

            using HttpResponseMessage response =
                await PostAsync(client, s_paypalPath, WebhookSigning.PayPalBody, headers);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            VerifyProcessed(Times.Never());
        }

        [Fact]
        public async Task PayPalWebhook_WithAValidSignatureForADifferentBody_Returns401AndProcessesNothing()
        {
            await using WebApplicationFactory<Program> factory = CreateFactory();
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await PostAsync(
                client,
                s_paypalPath,
                "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"id\":\"WH-2\"}",
                WebhookSigning.PayPalHeaders(_paypalKey, DateTimeOffset.UtcNow));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            VerifyProcessed(Times.Never());
        }

        [Fact]
        public async Task Webhook_WhenTheSecretIsNotConfigured_Returns500AndProcessesNothing()
        {
            Dictionary<string, string?> settings = new(WebhookSigning.Settings) { ["Stripe:WebhookSecret"] = string.Empty };
            await using WebApplicationFactory<Program> factory = CreateFactory(settings);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await PostAsync(
                client,
                s_stripePath,
                WebhookSigning.StripeBody,
                StripeHeaders(WebhookSigning.StripeBody, string.Empty, DateTimeOffset.UtcNow));

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            VerifyProcessed(Times.Never());
        }

        [Theory]
        [InlineData("HandleStripeWebhook")]
        [InlineData("HandlePayPalWebhook")]
        public void WebhookAction_IsAnonymousAndCapsTheRequestBody(string action)
        {
            System.Reflection.MethodInfo method = typeof(Controllers.BillingController).GetMethod(action)!;

            method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), true)
                .Should().ContainSingle();
            method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute), true)
                .Should().ContainSingle("an unauthenticated caller must not make the API buffer an unbounded body");
        }

        private static Dictionary<string, string> StripeHeaders(string body, string secret, DateTimeOffset signedAt)
        {
            return new Dictionary<string, string>
            {
                ["Stripe-Signature"] = WebhookSigning.StripeHeader(body, secret, signedAt),
            };
        }

        private static Dictionary<string, string> With(Dictionary<string, string> headers, string name, string value)
        {
            headers[name] = value;
            return headers;
        }

        private static Dictionary<string, string> Without(Dictionary<string, string> headers, string name)
        {
            headers.Remove(name);
            return headers;
        }

        private static async Task<HttpResponseMessage> PostAsync(
            HttpClient client,
            string path,
            string body,
            Dictionary<string, string> headers)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, path)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            foreach ((string name, string value) in headers)
            {
                request.Headers.Add(name, value);
            }

            return await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        private void VerifyProcessed(Times times)
        {
            // A payment_intent.succeeded or PAYMENT.CAPTURE.COMPLETED event makes the controller look up the invoice.
            _billing.Verify(
                b => b.GetCustomerInvoicesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                times);
        }

        private WebApplicationFactory<Program> CreateFactory(IDictionary<string, string?>? settings = null)
        {
            return ApiHostFactory.Create(
                settings ?? WebhookSigning.Settings,
                services =>
                {
                    services.ReplaceScoped(_billing.Object);
                    services.ReplaceSingleton<IPayPalCertificateProvider>(
                        new WebhookSigning.FixedCertificateProvider(_paypalCertificate));
                });
        }
    }
}
