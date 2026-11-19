namespace HotshotLogistics.Data.Repositories
{
    /// <summary>
    ///     Column lists and select fragments shared by the repositories. Queries name their columns instead of
    ///     selecting all of them, so a schema change cannot silently change what a mapper reads.
    /// </summary>
    internal static class EntityColumns
    {
        /// <summary>
        ///     Gets the SQL fragment for Customer.
        /// </summary>
        public const string Customer =
            "Id, CompanyName, TaxId, Email, Phone, BillingAddress, City, State, ZipCode, " +
            "Country, Latitude, Longitude, PaymentTermsDays, CreditStatus, CreditApprovedDate, " +
            "CreditExpiryDate, CreditLimit, IsActive, CreatedAt, UpdatedAt";

        /// <summary>
        ///     Gets the SQL fragment for CustomerAliased.
        /// </summary>
        public const string CustomerAliased =
            "c.Id, c.CompanyName, c.TaxId, c.Email, c.Phone, c.BillingAddress, c.City, c.State, " +
            "c.ZipCode, c.Country, c.Latitude, c.Longitude, c.PaymentTermsDays, c.CreditStatus, " +
            "c.CreditApprovedDate, c.CreditExpiryDate, c.CreditLimit, c.IsActive, c.CreatedAt, " +
            "c.UpdatedAt";

        /// <summary>
        ///     Gets the SQL fragment for Driver.
        /// </summary>
        public const string Driver =
            "Id, FirstName, LastName, Email, PhoneNumber, LicenseNumber, LicenseExpiryDate, " +
            "CurrentStatus, IsActive, CreatedAt, UpdatedAt";

        /// <summary>
        ///     Gets the SQL fragment for Invoice.
        /// </summary>
        public const string Invoice =
            "Id, InvoiceNumber, CustomerId, JobId, InvoiceDate, DueDate, Status, SubTotal, " +
            "TaxRate, TaxAmount, DiscountAmount, TotalAmount, PaidAmount, TermsDays, " +
            "EarlyPaymentDiscount, EarlyPaymentDiscountDays, LatePaymentPenalty, " +
            "LatePaymentPenaltyDays, Notes, CreatedAt, UpdatedAt";

        /// <summary>
        ///     Gets the SQL fragment for InvoiceAliased.
        /// </summary>
        public const string InvoiceAliased =
            "i.Id, i.InvoiceNumber, i.CustomerId, i.JobId, i.InvoiceDate, i.DueDate, i.Status, " +
            "i.SubTotal, i.TaxRate, i.TaxAmount, i.DiscountAmount, i.TotalAmount, i.PaidAmount, " +
            "i.TermsDays, i.EarlyPaymentDiscount, i.EarlyPaymentDiscountDays, " +
            "i.LatePaymentPenalty, i.LatePaymentPenaltyDays, i.Notes, i.CreatedAt, i.UpdatedAt";

        /// <summary>
        ///     Gets the SQL fragment for JobAssignment.
        /// </summary>
        public const string JobAssignment =
            "Id, JobId, DriverId, AssignedAt, Status";

        /// <summary>
        ///     Gets the SQL fragment for JobAssignmentJoined.
        /// </summary>
        public const string JobAssignmentJoined =
            @"
            SELECT ja.Id, ja.JobId, ja.DriverId, ja.AssignedAt, ja.Status,
                   d.FirstName as DriverFirstName, d.LastName as DriverLastName, d.Email as DriverEmail,
                   d.PhoneNumber as DriverPhoneNumber, d.LicenseNumber as DriverLicenseNumber, d.LicenseExpiryDate as DriverLicenseExpiryDate,
                   j.Title as JobTitle, j.PickupAddress as JobPickupAddress, j.DeliveryAddress as JobDeliveryAddress,
                   j.Status as JobStatus, j.Priority as JobPriority, j.TotalAmount as JobAmount,
                   j.EstimatedDeliveryTime as JobEstimatedDeliveryTime, j.CreatedAt as JobCreatedAt, j.UpdatedAt as JobUpdatedAt
            FROM JobAssignments ja
            LEFT JOIN Drivers d ON ja.DriverId = d.Id
            LEFT JOIN Jobs j ON ja.JobId = j.Id";

        /// <summary>
        ///     Gets the SQL fragment for Job.
        /// </summary>
        public const string Job =
            "Id, CustomerId, Title, PickupAddress, DeliveryAddress, Status, Priority, " +
            "TotalAmount, EstimatedDeliveryTime, ScheduledPickupTime, AssignedDriverId, " +
            "CreatedAt, UpdatedAt, SpecialInstructions";

        /// <summary>
        ///     Gets the SQL fragment for LocationTracking.
        /// </summary>
        public const string LocationTracking =
            "Id, JobId, DriverId, Latitude, Longitude, Speed, Heading, Accuracy, Timestamp";

        /// <summary>
        ///     Gets the SQL fragment for Payment.
        /// </summary>
        public const string Payment =
            "Id, InvoiceId, PaymentDate, Amount, PaymentMethod, TransactionId, " +
            "ProcessorResponse, Status, CreatedAt, UpdatedAt";
    }
}
