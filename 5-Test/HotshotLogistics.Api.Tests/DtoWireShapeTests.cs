using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HotshotLogistics.Api.Tests
{
    /// <summary>
    ///     Pins the JSON wire shape of every controller request and response type. The API serialises
    ///     with Newtonsoft, so each case round-trips a golden document through the serializer settings the
    ///     running host's MVC formatters use. A change to the type that alters a property name, a value
    ///     or what is omitted fails here.
    /// </summary>
    public class DtoWireShapeTests
    {
        private const string s_location =
            "{\"latitude\":40.5,\"longitude\":-74.25,\"speed\":55.5,\"heading\":90,\"accuracy\":5.0," +
            "\"timestamp\":\"2026-01-02T03:04:05Z\",\"status\":\"moving\",\"notes\":\"on route\"}";

        public static TheoryData<string, string> Documents => new()
        {
            { "ProcessPaymentRequest", "{\"amount\":125.5,\"paymentMethod\":\"Credit Card\",\"reference\":\"ref-1\"}" },
            {
                "PaymentResult",
                "{\"success\":true,\"invoiceId\":\"inv-1\",\"amount\":125.5,\"paymentMethod\":\"Credit Card\"," +
                "\"processedAt\":\"2026-01-02T03:04:05Z\"}"
            },
            { "TaxCalculationRequest", "{\"amount\":100.0,\"state\":\"CA\"}" },
            {
                "TaxCalculationResult",
                "{\"amount\":100.0,\"state\":\"CA\",\"taxAmount\":8.75,\"totalAmount\":108.75,\"taxRate\":0.0875}"
            },
            {
                "InvoiceSummaryMetricsResponse",
                "{\"totalInvoiced\":1000.0,\"totalPaid\":400.0,\"totalOutstanding\":600.0,\"overdueAmount\":150.0}"
            },
            {
                "InvoiceAgingBucketsResponse",
                "{\"current\":10.0,\"days30\":20.0,\"days60\":30.0,\"days90\":40.0,\"over90\":50.0,\"total\":150.0}"
            },
            {
                "AccountsReceivableReport",
                "{\"totalOverdueAmount\":150.0,\"overdueInvoiceCount\":1,\"generatedAt\":\"2026-01-02T03:04:05Z\"," +
                "\"overdueInvoices\":[{\"invoiceId\":\"inv-1\",\"invoiceNumber\":\"INV-1\",\"customerId\":\"cust-1\"," +
                "\"amount\":200.0,\"balanceDue\":150.0,\"dueDate\":\"2025-12-01T00:00:00Z\",\"daysOverdue\":32}]}"
            },
            {
                "OverdueInvoiceSummary",
                "{\"invoiceId\":\"inv-1\",\"invoiceNumber\":\"INV-1\",\"customerId\":\"cust-1\",\"amount\":200.0," +
                "\"balanceDue\":150.0,\"dueDate\":\"2025-12-01T00:00:00Z\",\"daysOverdue\":32}"
            },
            { "UpdateCreditLimitRequest", "{\"newLimit\":5000.0}" },
            { "AssignJobRequest", "{\"jobId\":\"job-1\",\"driverId\":7}" },
            { "AssignDriverRequest", "{\"driverId\":7}" },
            { "UpdateJobStatusRequest", "{\"status\":2}" },
            { "StartTrackingRequest", "{\"jobId\":\"job-1\",\"driverId\":7}" },
            { "UpdateLocationRequest", "{\"jobId\":\"job-1\",\"driverId\":7,\"locationUpdate\":" + s_location + "}" },
            { "RouteDeviationRequest", "{\"jobId\":\"job-1\",\"currentLocation\":" + s_location + "}" },
            {
                "TrackingResult",
                "{\"success\":true,\"jobId\":\"job-1\",\"driverId\":7,\"message\":\"started\"," +
                "\"timestamp\":\"2026-01-02T03:04:05Z\"}"
            },
            {
                "RouteDeviationResult",
                "{\"jobId\":\"job-1\",\"hasDeviated\":true,\"currentLocation\":" + s_location +
                ",\"checkedAt\":\"2026-01-02T03:04:05Z\",\"message\":\"off route\"}"
            },
            {
                "PublicTrackingInfo",
                "{\"jobId\":\"job-1\",\"currentLatitude\":40.5,\"currentLongitude\":-74.25," +
                "\"lastUpdated\":\"2026-01-02T03:04:05Z\",\"status\":\"In Transit\"," +
                "\"estimatedArrival\":\"2026-01-02T05:04:05Z\"}"
            },
        };

        [Theory]
        [MemberData(nameof(Documents))]
        public async Task RoundTrip_UsingTheHostsMvcSettings_PreservesTheWireShape(string typeName, string document)
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create();
            JsonSerializerSettings settings =
                factory.Services.GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>().Value.SerializerSettings;
            Type type = typeof(Program).Assembly.GetTypes().Single(candidate => candidate.Name == typeName);

            object? model = JsonConvert.DeserializeObject(document, type, settings);
            string written = JsonConvert.SerializeObject(model, settings);

            JToken.DeepEquals(JToken.Parse(written), JToken.Parse(document))
                .Should().BeTrue($"{typeName} wrote {written} for {document}");
        }

        [Fact]
        public async Task Serialize_WhenOptionalValuesAreNull_OmitsThem()
        {
            await using WebApplicationFactory<Program> factory = ApiHostFactory.Create();
            JsonSerializerSettings settings =
                factory.Services.GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>().Value.SerializerSettings;
            Type type = typeof(Program).Assembly.GetTypes().Single(candidate => candidate.Name == "TrackingResult");

            object? model = JsonConvert.DeserializeObject("{\"success\":false,\"jobId\":\"job-1\"}", type, settings);
            JObject written = JObject.Parse(JsonConvert.SerializeObject(model, settings));

            written.ContainsKey("driverId").Should().BeFalse("a null driver id is omitted from the wire shape");
            written["message"]!.Value<string>().Should().BeEmpty("a string defaults to empty, not null");
        }
    }
}
