// <copyright file="DriversController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Application.Authorization;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    ///     API controller for driver management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class DriversController : ApiControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly ILogger<DriversController> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DriversController" /> class.
        /// </summary>
        /// <param name="driverService">The driver service.</param>
        /// <param name="logger">The _logger.</param>
        public DriversController(
            IDriverService driverService,
            ILogger<DriversController> logger)
        {
            _driverService = driverService ?? throw new ArgumentNullException(nameof(driverService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Gets all drivers.
        /// </summary>
        /// <returns>A list of drivers.</returns>
        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<DriverDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<DriverDto>>> GetDrivers()
        {
            IEnumerable<Driver> drivers = await _driverService.GetDriversAsync();
            IEnumerable<DriverDto> driverDtos = drivers.Select(d => new DriverDto
            {
                Id = d.Id,
                FirstName = d.PersonalInfo.FirstName,
                LastName = d.PersonalInfo.LastName,
                Email = d.PersonalInfo.Email,
                PhoneNumber = d.PersonalInfo.PhoneNumber,
                LicenseNumber = d.License.LicenseNumber,
                LicenseExpiryDate = d.License.LicenseExpiryDate,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            });
            return Ok(driverDtos);
        }

        /// <summary>
        ///     Gets a driver by ID.
        /// </summary>
        /// <param name="id">The driver ID.</param>
        /// <returns>The driver if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DriverDto>> GetDriverById(int id)
        {
            Driver? driver = await _driverService.GetDriverByIdAsync(id);
            if (driver == null)
            {
                return NotFoundProblem($"Driver with ID {id} not found");
            }

            DriverDto driverDto = new()
            {
                Id = driver.Id,
                FirstName = driver.PersonalInfo.FirstName,
                LastName = driver.PersonalInfo.LastName,
                Email = driver.PersonalInfo.Email,
                PhoneNumber = driver.PersonalInfo.PhoneNumber,
                LicenseNumber = driver.License.LicenseNumber,
                LicenseExpiryDate = driver.License.LicenseExpiryDate,
                IsActive = driver.IsActive,
                CreatedAt = driver.CreatedAt,
                UpdatedAt = driver.UpdatedAt
            };
            return Ok(driverDto);
        }

        /// <summary>
        ///     Creates a new driver.
        /// </summary>
        /// <param name="driverDto">The driver data.</param>
        /// <returns>The created driver.</returns>
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(DriverDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DriverDto>> CreateDriver([FromBody] DriverDto? driverDto)
        {
            try
            {
                if (driverDto == null)
                {
                    return BadRequestProblem("Driver data is required");
                }

                Driver driver = new()
                {
                    PersonalInfo = new PersonalInfo
                    {
                        FirstName = driverDto.FirstName,
                        LastName = driverDto.LastName,
                        Email = driverDto.Email,
                        PhoneNumber = driverDto.PhoneNumber
                    },
                    License = new LicenseInfo
                    {
                        LicenseNumber = driverDto.LicenseNumber,
                        LicenseExpiryDate = driverDto.LicenseExpiryDate
                    },
                    IsActive = driverDto.IsActive
                };


                Driver createdDriver = await _driverService.CreateDriverAsync(driver);

                DriverDto createdDriverDto = new()
                {
                    Id = createdDriver.Id,
                    FirstName = createdDriver.PersonalInfo.FirstName,
                    LastName = createdDriver.PersonalInfo.LastName,
                    Email = createdDriver.PersonalInfo.Email,
                    PhoneNumber = createdDriver.PersonalInfo.PhoneNumber,
                    LicenseNumber = createdDriver.License.LicenseNumber,
                    LicenseExpiryDate = createdDriver.License.LicenseExpiryDate,
                    IsActive = createdDriver.IsActive,
                    CreatedAt = createdDriver.CreatedAt,
                    UpdatedAt = createdDriver.UpdatedAt
                };


                return CreatedAtAction(
                    nameof(GetDriverById),
                    new { id = createdDriver.Id },
                    createdDriverDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid driver data provided: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Updates an existing driver.
        /// </summary>
        /// <param name="id">The ID of the driver to update.</param>
        /// <param name="driverDto">The updated driver data.</param>
        /// <returns>The updated driver.</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(DriverDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DriverDto>> UpdateDriver(int id, [FromBody] DriverDto driverDto)
        {
            if (id != driverDto.Id)
            {
                return BadRequestProblem("Driver ID mismatch");
            }

            try
            {
                Driver driver = new()
                {
                    Id = driverDto.Id,
                    PersonalInfo = new PersonalInfo
                    {
                        FirstName = driverDto.FirstName,
                        LastName = driverDto.LastName,
                        Email = driverDto.Email,
                        PhoneNumber = driverDto.PhoneNumber
                    },
                    License = new LicenseInfo
                    {
                        LicenseNumber = driverDto.LicenseNumber,
                        LicenseExpiryDate = driverDto.LicenseExpiryDate
                    },
                    IsActive = driverDto.IsActive
                };

                Driver updatedDriver = await _driverService.UpdateDriverAsync(driver);

                DriverDto updatedDriverDto = new()
                {
                    Id = updatedDriver.Id,
                    FirstName = updatedDriver.PersonalInfo.FirstName,
                    LastName = updatedDriver.PersonalInfo.LastName,
                    Email = updatedDriver.PersonalInfo.Email,
                    PhoneNumber = updatedDriver.PersonalInfo.PhoneNumber,
                    LicenseNumber = updatedDriver.License.LicenseNumber,
                    LicenseExpiryDate = updatedDriver.License.LicenseExpiryDate,
                    IsActive = updatedDriver.IsActive,
                    CreatedAt = updatedDriver.CreatedAt,
                    UpdatedAt = updatedDriver.UpdatedAt
                };

                return Ok(updatedDriverDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequestProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Deletes a driver.
        /// </summary>
        /// <param name="id">The ID of the driver to delete.</param>
        /// <returns>A response indicating success or failure.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            bool success = await _driverService.DeleteDriverAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}
