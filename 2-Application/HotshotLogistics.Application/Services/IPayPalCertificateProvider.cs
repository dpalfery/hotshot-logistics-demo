// <copyright file="IPayPalCertificateProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Cryptography.X509Certificates;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Supplies the certificate PayPal signed a webhook with, from the URL in the request headers.
    /// </summary>
    public interface IPayPalCertificateProvider
    {
        /// <summary>
        ///     Gets the signing certificate if the URL is a PayPal URL and the certificate chains to a trusted root.
        /// </summary>
        /// <param name="certificateUrl">The <c>PAYPAL-CERT-URL</c> header value.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The certificate, or null when it cannot be trusted or fetched.</returns>
        Task<X509Certificate2?> GetCertificateAsync(Uri certificateUrl, CancellationToken cancellationToken = default);
    }
}
