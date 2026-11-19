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
        ///     Gets or sets the exact request bytes the signature was computed over. Null when a caller only has the
        ///     payload string; verification then uses that string's UTF-8 bytes. Not part of a stored document.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public byte[]? RawBody { get; set; }

        /// <summary>
        ///     The bytes a signature must cover: the raw body when the caller captured it, otherwise the UTF-8
        ///     bytes of <see cref="Payload" />.
        /// </summary>
        /// <returns>The body bytes.</returns>
        public byte[] BodyBytes()
        {
            return RawBody ?? System.Text.Encoding.UTF8.GetBytes(Payload ?? string.Empty);
        }

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
