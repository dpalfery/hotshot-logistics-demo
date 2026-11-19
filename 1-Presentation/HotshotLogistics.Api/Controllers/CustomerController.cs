// <copyright file="CustomerController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Api.Models;
using HotshotLogistics.Application.Authorization;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    ///     API controller for customer management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class CustomerController : ApiControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomerController" /> class.
        /// </summary>
        /// <param name="customerService">The customer service.</param>
        /// <param name="logger">The _logger.</param>
        public CustomerController(
            ICustomerService customerService,
            ILogger<CustomerController> logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Gets all customers.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of customers.</returns>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers(
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Customer> customers = await _customerService.GetCustomersAsync(cancellationToken);
            return Ok(customers);
        }

        /// <summary>
        ///     Gets a customer by ID.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The customer if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Customer>> GetCustomerById(string id,
            CancellationToken cancellationToken = default)
        {
            Customer? customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
            if (customer == null)
            {
                return NotFoundProblem($"Customer with ID {id} not found");
            }

            return Ok(customer);
        }

        /// <summary>
        ///     Creates a new customer.
        /// </summary>
        /// <param name="customer">The customer data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created customer.</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Customer), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Customer>> CreateCustomer(
            [FromBody] Customer? customer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (customer == null)
                {
                    return BadRequestProblem("Customer data is required");
                }

                Customer createdCustomer = await _customerService.CreateCustomerAsync(customer, cancellationToken);
                return CreatedAtAction(
                    nameof(GetCustomerById),
                    new { id = createdCustomer.Id },
                    createdCustomer);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid customer data provided: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Updates an existing customer.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="customer">The updated customer data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated customer.</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Customer>> UpdateCustomer(
            string id,
            [FromBody] Customer? customer,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (customer == null)
                {
                    return BadRequestProblem("Customer data is required");
                }

                // Ensure the ID in the URL matches the customer data
                customer.Id = id;

                Customer? updatedCustomer = await _customerService.UpdateCustomerAsync(id, customer, cancellationToken);
                if (updatedCustomer == null)
                {
                    return NotFoundProblem($"Customer with ID {id} not found");
                }

                return Ok(updatedCustomer);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid customer data provided: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Deletes a customer.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, 404 Not Found.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCustomer(string id, CancellationToken cancellationToken = default)
        {
            bool result = await _customerService.DeleteCustomerAsync(id, cancellationToken);
            if (!result)
            {
                return NotFoundProblem($"Customer with ID {id} not found");
            }

            return NoContent();
        }

        /// <summary>
        ///     Gets active customers.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of active customers.</returns>
        [HttpGet("active")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Customer>>> GetActiveCustomers(
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Customer> customers = await _customerService.GetActiveCustomersAsync(cancellationToken);
            return Ok(customers);
        }

        /// <summary>
        ///     Gets customers with overdue invoices.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of customers with overdue invoices.</returns>
        [HttpGet("overdue")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Customer>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Customer>>> GetOverdueCustomers(
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Customer> customers = await _customerService.GetOverdueCustomersAsync(cancellationToken);
            return Ok(customers);
        }

        /// <summary>
        ///     Gets jobs for a specific customer.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of jobs for the customer.</returns>
        [HttpGet("{id}/jobs")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(typeof(IEnumerable<Job>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<Job>>> GetCustomerJobs(string id,
            CancellationToken cancellationToken = default)
        {
            // First check if customer exists
            Customer? customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
            if (customer == null)
            {
                return NotFoundProblem($"Customer with ID {id} not found");
            }

            IEnumerable<Job> jobs = await _customerService.GetCustomerJobsAsync(id, cancellationToken);
            return Ok(jobs);
        }

        /// <summary>
        ///     Gets invoices for a specific customer.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of invoices for the customer.</returns>
        [HttpGet("{id}/invoices")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(typeof(IEnumerable<Invoice>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetCustomerInvoices(string id,
            CancellationToken cancellationToken = default)
        {
            // First check if customer exists
            Customer? customer = await _customerService.GetCustomerByIdAsync(id, cancellationToken);
            if (customer == null)
            {
                return NotFoundProblem($"Customer with ID {id} not found");
            }

            IEnumerable<Invoice> invoices = await _customerService.GetCustomerInvoicesAsync(id, cancellationToken);
            return Ok(invoices);
        }

        /// <summary>
        ///     Updates a customer's credit limit.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="request">The credit limit update request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, appropriate error response.</returns>
        [HttpPost("{id}/credit-limit")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCreditLimit(
            string id,
            [FromBody] UpdateCreditLimitRequest? request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return BadRequestProblem("Credit limit update request is required");
            }

            if (request.NewLimit < 0)
            {
                return BadRequestProblem("Credit limit cannot be negative");
            }

            bool result = await _customerService.UpdateCreditLimitAsync(id, request.NewLimit, cancellationToken);
            if (!result)
            {
                return NotFoundProblem($"Customer with ID {id} not found");
            }

            return NoContent();
        }

        /// <summary>
        ///     Updates a customer's credit terms.
        /// </summary>
        /// <param name="id">The customer ID.</param>
        /// <param name="creditTerms">The new credit terms.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful; otherwise, appropriate error response.</returns>
        [HttpPut("{id}/credit-terms")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCreditTerms(
            string id,
            [FromBody] CreditTerms? creditTerms,
            CancellationToken cancellationToken = default)
        {
            if (creditTerms == null)
            {
                return BadRequestProblem("Credit terms are required");
            }

            if (creditTerms.PaymentTermsDays <= 0)
            {
                return BadRequestProblem("Payment terms days must be greater than zero");
            }

            bool result = await _customerService.UpdateCreditTermsAsync(id, creditTerms, cancellationToken);
            if (!result)
            {
                return NotFoundProblem($"Customer with ID {id} not found");
            }

            return NoContent();
        }
    }
}
