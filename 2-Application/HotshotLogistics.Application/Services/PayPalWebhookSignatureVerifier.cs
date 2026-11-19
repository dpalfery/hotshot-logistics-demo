// <copyright file="PayPalWebhookSignatureVerifier.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Verifies a PayPal webhook the way PayPal documents offline verification: the
    ///     <c>PAYPAL-TRANSMISSION-SIG</c> header is a base64 RSA-SHA256 signature, made with the
    ///     certificate at <c>PAYPAL-CERT-URL</c>, over
    ///     <c>"{transmissionId}|{transmissionTime}|{webhookId}|{crc32 of the body}"</c>. A transmission time
    ///     outside the tolerance is rejected so a captured request cannot be replayed later.
    /// </summary>
    internal static class PayPalWebhookSignatureVerifier
    {
        /// <summary>
        ///     How old a transmission time may be.
        /// </summary>
        public static TimeSpan Tolerance { get; } = TimeSpan.FromMinutes(5);

        /// <summary>
        ///     How far ahead of the clock a transmission time may be.
        /// </summary>
        public static TimeSpan FutureSkew { get; } = TimeSpan.FromMinutes(1);

        private const string s_authAlgorithm = "SHA256withRSA";

        /// <summary>
        ///     Checks the headers of a webhook request against its body.
        /// </summary>
        /// <param name="payload">The exact request bytes.</param>
        /// <param name="headers">The request headers; names are matched ignoring case.</param>
        /// <param name="webhookId">The id of the webhook registered with PayPal.</param>
        /// <param name="certificates">Supplies the signing certificate.</param>
        /// <param name="now">The current time.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True when every header is present, the time is fresh and the signature verifies.</returns>
        public static async Task<bool> IsValidAsync(
            ReadOnlyMemory<byte> payload,
            IReadOnlyDictionary<string, string> headers,
            string webhookId,
            IPayPalCertificateProvider certificates,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(webhookId)
                || !TryGetHeader(headers, "PAYPAL-TRANSMISSION-ID", out string transmissionId)
                || !TryGetHeader(headers, "PAYPAL-TRANSMISSION-TIME", out string transmissionTime)
                || !TryGetHeader(headers, "PAYPAL-TRANSMISSION-SIG", out string signature)
                || !TryGetHeader(headers, "PAYPAL-CERT-URL", out string certificateUrl)
                || !TryGetHeader(headers, "PAYPAL-AUTH-ALGO", out string algorithm)
                || !string.Equals(algorithm, s_authAlgorithm, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!DateTimeOffset.TryParse(
                    transmissionTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset sentAt)
                || sentAt > now + FutureSkew
                || now - sentAt > Tolerance
                || !Uri.TryCreate(certificateUrl, UriKind.Absolute, out Uri? certificateUri))
            {
                return false;
            }

            uint checksum = Crc32(payload.Span);
            try
            {
                byte[] signatureBytes = Convert.FromBase64String(signature);
                using X509Certificate2? certificate =
                    await certificates.GetCertificateAsync(certificateUri, cancellationToken);
                using RSA? publicKey = certificate?.GetRSAPublicKey();
                if (publicKey is null)
                {
                    return false;
                }

                string message = $"{transmissionId}|{transmissionTime}|{webhookId}|{checksum}";
                return publicKey.VerifyData(
                    Encoding.UTF8.GetBytes(message),
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException)
            {
                return false;
            }
        }

        /// <summary>
        ///     Computes the CRC-32 (IEEE, as <c>java.util.zip.CRC32</c>) of the UTF-8 body, as PayPal's signed message uses it.
        /// </summary>
        /// <param name="payload">The exact request bytes.</param>
        /// <returns>The unsigned checksum.</returns>
        public static uint Crc32(ReadOnlySpan<byte> payload)
        {
            uint crc = uint.MaxValue;
            foreach (byte value in payload)
            {
                crc ^= value;
                for (int bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
                }
            }

            return ~crc;
        }

        private static bool TryGetHeader(IReadOnlyDictionary<string, string> headers, string name, out string value)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(header.Value))
                {
                    value = header.Value;
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }
    }
}
