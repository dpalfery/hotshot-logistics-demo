using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents payment method details.
    /// </summary>
    public class PaymentMethodDetails
    {
        /// <summary>
        ///     Gets or sets the payment method type.
        /// </summary>
        public PaymentMethodType Type { get; set; }

        /// <summary>
        ///     Gets or sets the token or reference for the payment method.
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets additional payment method data.
        /// </summary>
        public Dictionary<string, string> AdditionalData { get; set; } = new();
    }
}
