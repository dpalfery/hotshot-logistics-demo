// <copyright file="StripeWebhookSignatureVerifier.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Verifies the <c>Stripe-Signature</c> header: <c>t=&lt;unix seconds&gt;,v1=&lt;hex HMAC-SHA256&gt;</c>, where the
    ///     HMAC covers <c>"{t}.{payload}"</c> keyed with the endpoint's webhook secret. A timestamp
    ///     outside the tolerance is rejected so a captured request cannot be replayed later.
    /// </summary>
    internal static class StripeWebhookSignatureVerifier
    {
        /// <summary>
        ///     How old a signed timestamp may be. This is Stripe's default.
        /// </summary>
        public static TimeSpan Tolerance { get; } = TimeSpan.FromMinutes(5);

        /// <summary>
        ///     How far ahead of the clock a timestamp may be. A captured request stamped in the future is not
        ///     accepted for the whole tolerance window.
        /// </summary>
        public static TimeSpan FutureSkew { get; } = TimeSpan.FromMinutes(1);

        /// <summary>
        ///     Checks a signature header against the raw request body.
        /// </summary>
        /// <param name="payload">The exact request bytes.</param>
        /// <param name="signatureHeader">The <c>Stripe-Signature</c> header value.</param>
        /// <param name="secret">The webhook signing secret.</param>
        /// <param name="now">The current time.</param>
        /// <returns>True when a <c>v1</c> signature matches and the timestamp is within the tolerance.</returns>
        public static bool IsValid(ReadOnlySpan<byte> payload, string signatureHeader, string secret, DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(signatureHeader))
            {
                return false;
            }

            string? timestamp = null;
            List<byte[]> candidates = [];
            foreach (string part in signatureHeader.Split(','))
            {
                string[] pair = part.Trim().Split('=', 2);
                if (pair.Length != 2)
                {
                    continue;
                }

                if (pair[0] == "t")
                {
                    timestamp = pair[1];
                }
                else if (pair[0] == "v1" && TryDecodeHex(pair[1], out byte[] bytes))
                {
                    candidates.Add(bytes);
                }
            }

            if (timestamp is null
                || candidates.Count == 0
                || !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out long seconds)
                || seconds > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            {
                return false;
            }

            DateTimeOffset sentAt = DateTimeOffset.FromUnixTimeSeconds(seconds);
            if (sentAt > now + FutureSkew || now - sentAt > Tolerance)
            {
                return false;
            }

            byte[] prefix = Encoding.ASCII.GetBytes($"{timestamp}.");
            byte[] signed = new byte[prefix.Length + payload.Length];
            prefix.CopyTo(signed, 0);
            payload.CopyTo(signed.AsSpan(prefix.Length));
            byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signed);
            return candidates.Exists(candidate => CryptographicOperations.FixedTimeEquals(candidate, expected));
        }

        private static bool TryDecodeHex(string value, out byte[] bytes)
        {
            try
            {
                bytes = Convert.FromHexString(value);
                return true;
            }
            catch (FormatException)
            {
                bytes = [];
                return false;
            }
        }
    }
}
