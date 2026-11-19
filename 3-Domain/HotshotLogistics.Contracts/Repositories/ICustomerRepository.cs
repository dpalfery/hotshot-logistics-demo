using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;

namespace HotshotLogistics.Contracts.Repositories
{
    /// <summary>
    ///     Repository interface for customer data access operations.
    /// </summary>
    public interface ICustomerRepository : IRepository<Customer>
    {
        /// <summary>
        ///     Gets a customer by their tax identification number.
        /// </summary>
        /// <param name="taxId">The tax identification number.</param>
        /// <returns>The customer if found, null otherwise.</returns>
        Task<Customer?> GetByTaxIdAsync(string taxId);

        /// <summary>
        ///     Gets customers within a credit limit range.
        /// </summary>
        /// <param name="minLimit">The minimum credit limit.</param>
        /// <param name="maxLimit">The maximum credit limit.</param>
        /// <returns>A list of customers within the specified credit limit range.</returns>
        Task<IEnumerable<Customer>> GetByCreditLimitRangeAsync(decimal minLimit, decimal maxLimit);

        /// <summary>
        ///     Gets all active customers.
        /// </summary>
        /// <returns>A list of active customers.</returns>
        Task<IEnumerable<Customer>> GetActiveCustomersAsync();

        /// <summary>
        ///     Gets customers with overdue invoices.
        /// </summary>
        /// <returns>A list of customers with overdue invoices.</returns>
        Task<IEnumerable<Customer>> GetOverdueCustomersAsync();

        /// <summary>
        ///     Gets the total credit limit for all active customers.
        /// </summary>
        /// <returns>The total credit limit.</returns>
        Task<decimal> GetTotalCreditLimitAsync();

        /// <summary>
        ///     Gets the count of active customers.
        /// </summary>
        /// <returns>The number of active customers.</returns>
        Task<int> GetCustomerCountAsync();

        /// <summary>
        ///     Updates a customer's credit limit.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="newLimit">The new credit limit.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateCreditLimitAsync(string customerId, decimal newLimit);

        /// <summary>
        ///     Deactivates a customer account.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <returns>True if the deactivation was successful, false otherwise.</returns>
        Task<bool> DeactivateCustomerAsync(string customerId);

        /// <summary>
        ///     Reactivates a customer account.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <returns>True if the reactivation was successful, false otherwise.</returns>
        Task<bool> ReactivateCustomerAsync(string customerId);

        /// <summary>
        ///     Updates a customer's credit terms.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="creditTerms">The new credit terms.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateCreditTermsAsync(string customerId, CreditTerms creditTerms);
    }
}
