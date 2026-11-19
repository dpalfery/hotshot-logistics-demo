using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HotshotLogistics.Application.Services;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Signs webhook requests the way Stripe and PayPal document, independently of the production verifiers.
    ///     Every secret here is a throwaway value that exists only inside the test host.
    /// </summary>
    internal static class WebhookSigning
    {
        public const string StripeSecret = "test-stripe-webhook-secret";
        public const string PayPalWebhookId = "test-paypal-webhook-id";
        public const string StripeBody = "{\"type\":\"payment_intent.succeeded\"}";

        /// <summary>
        ///     A PayPal body whose CRC-32 (254039784) was computed with Python's zlib, not by the code under test.
        /// </summary>
        public const string PayPalBody = "{\"event_type\":\"PAYMENT.CAPTURE.COMPLETED\",\"id\":\"WH-1\"}";

        private const uint s_payPalBodyCrc = 254039784;

        public static IDictionary<string, string?> Settings { get; } = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = string.Empty,
            ["Stripe:ApiKey"] = "test-stripe-api-key",
            ["Stripe:WebhookSecret"] = StripeSecret,
            ["PayPal:ClientId"] = "test-paypal-client-id",
            ["PayPal:ClientSecret"] = "test-paypal-client-secret",
            ["PayPal:WebhookId"] = PayPalWebhookId,
        };

        public static string StripeHeader(string body, string secret, DateTimeOffset signedAt)
        {
            string timestamp = signedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            byte[] mac = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}"));
            return $"t={timestamp},v1={Convert.ToHexString(mac).ToLowerInvariant()}";
        }

        /// <summary>
        ///     Builds the five PayPal headers for <see cref="PayPalBody" />.
        /// </summary>
        public static Dictionary<string, string> PayPalHeaders(RSA signingKey, DateTimeOffset sentAt)
        {
            string id = "transmission-1";
            string time = sentAt.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            string message = $"{id}|{time}|{PayPalWebhookId}|{s_payPalBodyCrc}";
            byte[] signature = signingKey.SignData(
                Encoding.UTF8.GetBytes(message),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            return new Dictionary<string, string>
            {
                ["PAYPAL-TRANSMISSION-ID"] = id,
                ["PAYPAL-TRANSMISSION-TIME"] = time,
                ["PAYPAL-TRANSMISSION-SIG"] = Convert.ToBase64String(signature),
                ["PAYPAL-CERT-URL"] = "https://api.sandbox.paypal.com/v1/notifications/certs/CERT-TEST",
                ["PAYPAL-AUTH-ALGO"] = "SHA256withRSA",
            };
        }

        public static X509Certificate2 CreateCertificate(RSA key)
        {
            CertificateRequest request = new("CN=paypal-webhook-test", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        }

        /// <summary>
        ///     A certificate provider that hands out one fixed certificate for any URL.
        /// </summary>
        public sealed class FixedCertificateProvider(X509Certificate2 certificate) : IPayPalCertificateProvider
        {
            public Task<X509Certificate2?> GetCertificateAsync(Uri certificateUrl, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<X509Certificate2?>(new X509Certificate2(certificate));
            }
        }
    }
}
