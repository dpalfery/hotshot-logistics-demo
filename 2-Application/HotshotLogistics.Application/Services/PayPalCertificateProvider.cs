// <copyright file="PayPalCertificateProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Downloads PayPal's webhook signing certificate. The URL comes from an unauthenticated request, so
    ///     it is fetched only when it is an https URL on a <c>paypal.com</c> host (never an attacker-chosen
    ///     address), and the certificate is used only when it chains to a trusted root.
    /// </summary>
    public sealed class PayPalCertificateProvider : IPayPalCertificateProvider
    {
        private const int s_maxCertificateBytes = 32 * 1024;
        private const int s_maxCachedCertificates = 16;
        private static readonly TimeSpan s_downloadTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan s_cacheLifetime = TimeSpan.FromHours(1);

        private readonly ConcurrentDictionary<Uri, (X509Certificate2 Certificate, DateTimeOffset ExpiresAt)> _cache = new();
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PayPalCertificateProvider> _logger;
        private readonly TimeProvider _timeProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PayPalCertificateProvider" /> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="timeProvider">The clock; the system clock when omitted.</param>
        public PayPalCertificateProvider(
            IHttpClientFactory httpClientFactory,
            ILogger<PayPalCertificateProvider> logger,
            TimeProvider? timeProvider = null)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <summary>
        ///     Checks that a certificate URL is one this service may fetch.
        /// </summary>
        /// <param name="url">The URL from the request header.</param>
        /// <returns>True for an https URL, without credentials or a custom port, on a paypal.com host.</returns>
        public static bool IsTrustedCertificateUrl(Uri url)
        {
            ArgumentNullException.ThrowIfNull(url);
            return url.IsAbsoluteUri
                   && url.Scheme == Uri.UriSchemeHttps
                   && url.IsDefaultPort
                   && string.IsNullOrEmpty(url.UserInfo)
                   && url.Host.EndsWith(".paypal.com", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<X509Certificate2?> GetCertificateAsync(
            Uri certificateUrl,
            CancellationToken cancellationToken = default)
        {
            if (!IsTrustedCertificateUrl(certificateUrl))
            {
                _logger.LogWarning("Refusing to fetch a PayPal webhook certificate from an untrusted URL");
                return null;
            }

            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (_cache.TryGetValue(certificateUrl, out (X509Certificate2 Certificate, DateTimeOffset ExpiresAt) cached)
                && cached.ExpiresAt > now)
            {
                return new X509Certificate2(cached.Certificate);
            }

            try
            {
                X509Certificate2Collection chainCertificates = await DownloadAsync(certificateUrl, cancellationToken);
                if (chainCertificates.Count == 0 || !IsTrusted(chainCertificates))
                {
                    _logger.LogWarning("The PayPal webhook certificate does not chain to a trusted root");
                    return null;
                }

                if (_cache.Count >= s_maxCachedCertificates)
                {
                    _cache.Clear();
                }

                X509Certificate2 leaf = new(chainCertificates[0]);
                _cache[certificateUrl] = (leaf, now + s_cacheLifetime);
                return new X509Certificate2(leaf);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or CryptographicException or InvalidOperationException
                                           or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not fetch the PayPal webhook certificate");
                return null;
            }
        }

        private static bool IsTrusted(X509Certificate2Collection certificates)
        {
            using X509Chain chain = new();

            // Revocation lookups would make an unauthenticated request depend on a second outbound call.
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            for (int index = 1; index < certificates.Count; index++)
            {
                chain.ChainPolicy.ExtraStore.Add(certificates[index]);
            }

            return chain.Build(certificates[0]);
        }

        private async Task<X509Certificate2Collection> DownloadAsync(Uri url, CancellationToken cancellationToken)
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(s_downloadTimeout);

            using HttpClient client = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength > s_maxCertificateBytes)
            {
                throw new InvalidOperationException("The certificate response is too large.");
            }

            string pem = await response.Content.ReadAsStringAsync(timeout.Token);
            if (pem.Length > s_maxCertificateBytes)
            {
                throw new InvalidOperationException("The certificate response is too large.");
            }

            X509Certificate2Collection certificates = [];
            certificates.ImportFromPem(pem);
            return certificates;
        }
    }
}
