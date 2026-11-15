// <copyright file="AzureMapsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotshotLogistics.Data.Services
{
    /// <summary>
    ///     Azure Maps implementation of the mapping service.
    /// </summary>
    public class AzureMapsService : IMappingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AzureMapsService> _logger;
        private readonly string _subscriptionKey;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzureMapsService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for API calls.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="settings">The Azure Maps settings.</param>
        public AzureMapsService(HttpClient httpClient, ILogger<AzureMapsService> logger,
            IOptions<AzureMapsSettings> settings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ArgumentNullException.ThrowIfNull(settings);
            AzureMapsSettings settingsValue = settings.Value;
            _subscriptionKey = settingsValue.SubscriptionKey ??
                               throw new ArgumentNullException(nameof(settingsValue.SubscriptionKey));
        }

        /// <inheritdoc />
        public async Task<GeocodingResult> GeocodeAddressAsync(string address,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string url =
                    $"https://atlas.microsoft.com/search/address/json?api-version=1.0&subscription-key={_subscriptionKey}&query={Uri.EscapeDataString(address)}";

                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                AzureMapsGeocodeResponse? result = JsonSerializer.Deserialize<AzureMapsGeocodeResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Results.Count > 0)
                {
                    GeocodeResultItem firstResult = result.Results[0];
                    return new GeocodingResult
                    {
                        Latitude = firstResult.Position.Lat,
                        Longitude = firstResult.Position.Lon,
                        FormattedAddress = firstResult.Address.FreeformAddress.Length > 0
                            ? firstResult.Address.FreeformAddress
                            : address,
                        IsValid = true,
                        Confidence = firstResult.Confidence ?? 0.0
                    };
                }

                return new GeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = "No geocoding results found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to geocode address: {Address}", address);
                return new GeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc />
        public async Task<ReverseGeocodingResult> ReverseGeocodeAsync(decimal latitude, decimal longitude,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string url =
                    $"https://atlas.microsoft.com/search/address/reverse/json?api-version=1.0&subscription-key={_subscriptionKey}&query={latitude},{longitude}";

                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                AzureMapsReverseGeocodeResponse? result =
                    JsonSerializer.Deserialize<AzureMapsReverseGeocodeResponse>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Addresses.Count > 0)
                {
                    ReverseGeocodeAddress address = result.Addresses[0];
                    return new ReverseGeocodingResult
                    {
                        Address = address.Address.StreetName,
                        City = address.Address.Municipality,
                        State = address.Address.CountrySubdivision,
                        PostalCode = address.Address.PostalCode,
                        Country = address.Address.CountryCode,
                        FormattedAddress = address.Address.FreeformAddress,
                        IsValid = true
                    };
                }

                return new ReverseGeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = "No reverse geocoding results found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reverse geocode coordinates");
                return new ReverseGeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc />
        public async Task<bool> ValidateAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            GeocodingResult result = await GeocodeAddressAsync(address, cancellationToken);
            return result is { IsValid: true, Confidence: > 0.7 }; // Require high confidence for validation
        }

        /// <inheritdoc />
        public async Task<RouteResult> CalculateRouteAsync(Location origin, Location destination,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!origin.HasCoordinates || !destination.HasCoordinates)
                {
                    return new RouteResult
                    {
                        IsValid = false,
                        ErrorMessage = "Both origin and destination must have coordinates"
                    };
                }

                string url =
                    $"https://atlas.microsoft.com/route/directions/json?api-version=1.0&subscription-key={_subscriptionKey}&query={origin.Latitude},{origin.Longitude}:{destination.Latitude},{destination.Longitude}";

                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                AzureMapsRouteResponse? result = JsonSerializer.Deserialize<AzureMapsRouteResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Routes.Count > 0)
                {
                    RouteItem route = result.Routes[0];
                    RouteSummary summary = route.Summary;

                    return new RouteResult
                    {
                        Distance = summary.LengthInMeters * 0.000621371, // Convert meters to miles
                        Duration = TimeSpan.FromSeconds(summary.TravelTimeInSeconds),
                        Waypoints = [origin, destination],
                        Polyline = string.Empty, // Azure Maps doesn't provide polyline in basic response
                        EstimatedArrival = DateTime.UtcNow.AddSeconds(summary.TravelTimeInSeconds),
                        IsValid = true
                    };
                }

                return new RouteResult
                {
                    IsValid = false,
                    ErrorMessage = "No route found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate route between {Origin} and {Destination}", origin.FullAddress,
                    destination.FullAddress);
                return new RouteResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc />
        public async Task<OptimizedRouteResult> OptimizeRouteAsync(IList<Location> waypoints,
            CancellationToken cancellationToken = default)
        {
            // For simplicity, return the waypoints in order without optimization
            // In a real implementation, this would use Azure Maps Route Optimization API
            try
            {
                if (waypoints.Count < 2)
                {
                    return new OptimizedRouteResult
                    {
                        IsValid = false,
                        ErrorMessage = "At least 2 waypoints required"
                    };
                }

                double totalDistance = 0.0;
                TimeSpan totalDuration = TimeSpan.Zero;
                List<RouteResult> segments = new();

                for (int i = 0; i < waypoints.Count - 1; i++)
                {
                    RouteResult segment = await CalculateRouteAsync(waypoints[i], waypoints[i + 1], cancellationToken);
                    if (!segment.IsValid)
                    {
                        return new OptimizedRouteResult
                        {
                            IsValid = false,
                            ErrorMessage = $"Failed to calculate route segment {i}"
                        };
                    }

                    totalDistance += segment.Distance;
                    totalDuration = totalDuration.Add(segment.Duration);
                    segments.Add(segment);
                }

                return new OptimizedRouteResult
                {
                    OptimizedWaypoints = waypoints,
                    TotalDistance = totalDistance,
                    TotalDuration = totalDuration,
                    RouteSegments = segments,
                    EstimatedArrival = DateTime.UtcNow.Add(totalDuration),
                    IsValid = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize route with {WaypointCount} waypoints", waypoints.Count);
                return new OptimizedRouteResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <inheritdoc />
        public async Task<DistanceResult> CalculateDistanceAsync(Location origin, Location destination,
            CancellationToken cancellationToken = default)
        {
            RouteResult route = await CalculateRouteAsync(origin, destination, cancellationToken);
            return new DistanceResult
            {
                Distance = route.Distance,
                Duration = route.Duration,
                IsValid = route.IsValid,
                ErrorMessage = route.ErrorMessage
            };
        }

        // Internal classes for Azure Maps API responses
        private sealed class AzureMapsGeocodeResponse
        {
            public List<GeocodeResultItem> Results { get; init; } = [];
        }

        private sealed class GeocodeResultItem
        {
            public Position Position { get; init; } = new();
            public AzureMapsAddress Address { get; init; } = new();
            public double? Confidence { get; init; }
        }

        private sealed class Position
        {
            public decimal Lat { get; init; }
            public decimal Lon { get; init; }
        }

        private sealed class AzureMapsAddress
        {
            public string FreeformAddress { get; init; } = string.Empty;
        }

        private sealed class AzureMapsReverseGeocodeResponse
        {
            public List<ReverseGeocodeAddress> Addresses { get; init; } = [];
        }

        private sealed class ReverseGeocodeAddress
        {
            public AzureMapsReverseAddress Address { get; init; } = new();
        }

        private sealed class AzureMapsReverseAddress
        {
            public string StreetName { get; init; } = string.Empty;
            public string Municipality { get; init; } = string.Empty;
            public string CountrySubdivision { get; init; } = string.Empty;
            public string PostalCode { get; init; } = string.Empty;
            public string CountryCode { get; init; } = string.Empty;
            public string FreeformAddress { get; init; } = string.Empty;
        }

        private sealed class AzureMapsRouteResponse
        {
            public List<RouteItem> Routes { get; init; } = [];
        }

        private sealed class RouteItem
        {
            public RouteSummary Summary { get; init; } = new();
        }

        private sealed class RouteSummary
        {
            public double LengthInMeters { get; init; }
            public double TravelTimeInSeconds { get; init; }
        }
    }
}
