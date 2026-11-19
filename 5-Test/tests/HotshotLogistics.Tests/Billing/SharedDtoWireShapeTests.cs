using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HotshotLogistics.Tests.Billing
{
    /// <summary>
    ///     Pins the JSON property names of the shared billing and notification DTOs. A class rename must not change
    ///     what the API serializer writes or what a stored or posted payload has to contain.
    /// </summary>
    public class SharedDtoWireShapeTests
    {
        // The API registers Newtonsoft with these settings (Program.cs); MVC's Newtonsoft formatter camel-cases names by default
        private static readonly JsonSerializerSettings s_apiSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        public static TheoryData<string, object, string[]> Shapes() => new()
        {
            {
                nameof(InvoiceFilterDto),
                new InvoiceFilterDto
                {
                    CustomerId = "c", JobId = "j", Status = InvoiceStatus.Sent,
                    StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow, MinAmount = 1m, MaxAmount = 2m,
                    SearchTerm = "s", IsOverdue = true
                },
                new[]
                {
                    "customerId", "jobId", "status", "startDate", "endDate", "minAmount", "maxAmount", "searchTerm",
                    "isOverdue", "pageNumber", "pageSize", "sortBy", "sortDirection"
                }
            },
            {
                nameof(AgingReportEntryDto),
                new AgingReportEntryDto(),
                new[]
                {
                    "customerId", "customerName", "current", "days1To30", "days31To60", "days61To90", "over90Days",
                    "totalBalance"
                }
            },
            {
                nameof(InvoiceSummaryDto),
                new InvoiceSummaryDto(),
                new[]
                {
                    "totalInvoices", "totalAmount", "totalPaid", "totalOutstanding", "overdueCount", "overdueAmount",
                    "averageDaysToPayment"
                }
            },
            {
                nameof(NotificationPreferencesDto),
                new NotificationPreferencesDto { PhoneNumber = "p", EmailAddress = "e", DeviceToken = "d" },
                new[]
                {
                    "userId", "smsEnabled", "emailEnabled", "pushEnabled", "phoneNumber", "emailAddress", "deviceToken",
                    "enabledNotificationTypes"
                }
            },
            {
                nameof(PaymentMethodDetailsDto),
                new PaymentMethodDetailsDto(),
                new[] { "type", "token", "additionalData" }
            },
            {
                nameof(PaymentProcessingResultDto),
                new PaymentProcessingResultDto { ErrorCode = "e" },
                new[] { "success", "transactionId", "message", "metadata", "errorCode" }
            },
            {
                nameof(WebhookEventDataDto),
                new WebhookEventDataDto(),
                new[] { "eventType", "payload", "signature", "headers" }
            },
            {
                nameof(WebhookProcessingResultDto),
                new WebhookProcessingResultDto { StatusUpdate = new PaymentStatusUpdateDto() },
                new[] { "success", "signatureRejected", "statusUpdate", "message" }
            },
            {
                nameof(PaymentStatusUpdateDto),
                new PaymentStatusUpdateDto(),
                new[] { "transactionId", "status", "invoiceId", "amount" }
            }
        };

        /// <summary>
        ///     The API serializer writes exactly these property names for the shared DTOs.
        /// </summary>
        [Theory]
        [MemberData(nameof(Shapes))]
        public void Serialize_SharedDto_WritesTheDocumentedPropertyNames(string name, object dto, string[] expected)
        {
            JObject json = JObject.Parse(JsonConvert.SerializeObject(dto, s_apiSettings));

            string[] actual = json.Properties().Select(p => p.Name).ToArray();

            Assert.True(
                expected.Order().SequenceEqual(actual.Order()),
                $"{name} wrote [{string.Join(", ", actual)}]");
        }

        /// <summary>
        ///     A stored preferences document (written before the rename) still deserializes into the renamed type.
        /// </summary>
        [Fact]
        public void Deserialize_StoredNotificationPreferences_ReadsTheStoredProperties()
        {
            const string stored =
                "{\"UserId\":\"u1\",\"SmsEnabled\":true,\"EmailEnabled\":false,\"PushEnabled\":true,"
                + "\"PhoneNumber\":\"555\",\"EmailAddress\":\"a@b.c\",\"DeviceToken\":\"t\",\"EnabledNotificationTypes\":[3]}";

            NotificationPreferencesDto? preferences =
                System.Text.Json.JsonSerializer.Deserialize<NotificationPreferencesDto>(stored);

            Assert.NotNull(preferences);
            Assert.Equal("u1", preferences.UserId);
            Assert.True(preferences.SmsEnabled);
            Assert.False(preferences.EmailEnabled);
            Assert.True(preferences.PushEnabled);
            Assert.Equal("555", preferences.PhoneNumber);
            Assert.Equal("a@b.c", preferences.EmailAddress);
            Assert.Equal("t", preferences.DeviceToken);
            Assert.Single(preferences.EnabledNotificationTypes);
        }
    }
}
