// <copyright file="PayPalCertificateProviderTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using HotshotLogistics.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
    ///     Tests for <see cref="PayPalCertificateProvider" />: it fetches only from PayPal and trusts only a chained certificate.
    /// </summary>
    public class PayPalCertificateProviderTests
    {
        [Theory]
        [InlineData("https://api.paypal.com/v1/notifications/certs/CERT-1", true)]
        [InlineData("https://api.sandbox.paypal.com/v1/notifications/certs/CERT-1", true)]
        [InlineData("http://api.paypal.com/v1/notifications/certs/CERT-1", false)]
        [InlineData("https://paypal.com.evil.example/certs/CERT-1", false)]
        [InlineData("https://evilpaypal.com/certs/CERT-1", false)]
        [InlineData("https://api.paypal.com@evil.example/certs/CERT-1", false)]
        [InlineData("https://user:pass@api.paypal.com/certs/CERT-1", false)]
        [InlineData("https://api.paypal.com:8443/certs/CERT-1", false)]
        [InlineData("https://169.254.169.254/latest/meta-data", false)]
        public void IsTrustedCertificateUrl_OnlyAcceptsPlainHttpsPayPalHosts(string url, bool expected)
        {
            PayPalCertificateProvider.IsTrustedCertificateUrl(new Uri(url)).Should().Be(expected);
        }

        [Fact]
        public async Task GetCertificateAsync_ForAnUntrustedHost_ReturnsNullWithoutMakingARequest()
        {
            RecordingHandler handler = new(string.Empty);
            PayPalCertificateProvider provider = CreateProvider(handler);

            X509Certificate2? certificate = await provider.GetCertificateAsync(
                new Uri("https://evil.example/certs/CERT-1"),
                TestContext.Current.CancellationToken);

            certificate.Should().BeNull();
            handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task GetCertificateAsync_ForACertificateThatDoesNotChainToATrustedRoot_ReturnsNull()
        {
            using RSA key = RSA.Create(2048);
            CertificateRequest request = new("CN=self-signed", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using X509Certificate2 selfSigned = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddDays(1));
            RecordingHandler handler = new(selfSigned.ExportCertificatePem());
            PayPalCertificateProvider provider = CreateProvider(handler);

            X509Certificate2? certificate = await provider.GetCertificateAsync(
                new Uri("https://api.sandbox.paypal.com/v1/notifications/certs/CERT-1"),
                TestContext.Current.CancellationToken);

            handler.Requests.Should().ContainSingle("the PayPal URL is fetched");
            certificate.Should().BeNull("a self-signed certificate is not trusted");
        }

        [Fact]
        public async Task GetCertificateAsync_WhenPayPalAnswersWithAnError_ReturnsNull()
        {
            RecordingHandler handler = new(string.Empty, HttpStatusCode.InternalServerError);
            PayPalCertificateProvider provider = CreateProvider(handler);

            X509Certificate2? certificate = await provider.GetCertificateAsync(
                new Uri("https://api.paypal.com/v1/notifications/certs/CERT-1"),
                TestContext.Current.CancellationToken);

            certificate.Should().BeNull();
        }

        private static PayPalCertificateProvider CreateProvider(HttpMessageHandler handler)
        {
            Mock<IHttpClientFactory> factory = new();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, false));
            return new PayPalCertificateProvider(factory.Object, NullLogger<PayPalCertificateProvider>.Instance);
        }

        private sealed class RecordingHandler(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
        {
            public List<Uri?> Requests { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request.RequestUri);
                return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
            }
        }
    }
}
