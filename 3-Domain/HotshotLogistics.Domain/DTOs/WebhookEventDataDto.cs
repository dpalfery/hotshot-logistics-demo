namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents webhook event data.
    /// </summary>
    public class WebhookEventDataDto
    {
        /// <summary>
        ///     Gets or sets the event type.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the event data payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the webhook signature.
        /// </summary>
        public string Signature { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets additional headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();
    }
}
