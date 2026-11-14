// <copyright file="GoogleMapsService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotshotLogistics.Data.Services
{
    /// <summary>
    ///     Google Maps implementation of the mapping service.
    /// </summary>
    public class GoogleMapsService : IMappingService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleMapsService> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GoogleMapsService" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for API calls.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="settings">The Google Maps settings.</param>
        public GoogleMapsService(HttpClient httpClient, ILogger<GoogleMapsService> logger,
            IOptions<GoogleMapsSettings> settings)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ArgumentNullException.ThrowIfNull(settings);
            GoogleMapsSettings googleMapsSettings = settings.Value;
            _apiKey = googleMapsSettings.ApiKey ?? throw new ArgumentNullException(nameof(googleMapsSettings.ApiKey));
        }

        /// <inheritdoc />
        public async Task<GeocodingResult> GeocodeAddressAsync(string address,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string url =
                    $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_apiKey}";

                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                GoogleMapsGeocodeResponse? result = JsonSerializer.Deserialize<GoogleMapsGeocodeResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result is { Status: "OK", Results.Count: > 0 })
                {
                    GeocodeResult firstResult = result.Results[0];
                    Coordinate location = firstResult.Geometry.Location;

                    return new GeocodingResult
                    {
                        Latitude = location.Lat,
                        Longitude = location.Lng,
                        FormattedAddress = firstResult.FormattedAddress,
                        IsValid = true,
                        Confidence = 1.0 // Google Maps doesn't provide confidence scores
                    };
                }

                return new GeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = result?.Status ?? "Geocoding failed"
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
                    $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={_apiKey}";

                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                GoogleMapsGeocodeResponse? result = JsonSerializer.Deserialize<GoogleMapsGeocodeResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result is { Status: "OK", Results.Count: > 0 })
                {
                    GeocodeResult firstResult = result.Results[0];
                    List<AddressComponent> components = firstResult.AddressComponents;

                    return new ReverseGeocodingResult
                    {
                        Address = GetAddressComponent(components, "street_number") + " " +
                                  GetAddressComponent(components, "route"),
                        City = GetAddressComponent(components, "locality"),
                        State = GetAddressComponent(components, "administrative_area_level_1"),
                        PostalCode = GetAddressComponent(components, "postal_code"),
                        Country = GetAddressComponent(components, "country"),
                        FormattedAddress = firstResult.FormattedAddress,
                        IsValid = true
                    };
                }

                return new ReverseGeocodingResult
                {
                    IsValid = false,
                    ErrorMessage = result?.Status ?? "Reverse geocoding failed"
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
            return result.IsValid;
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
                    $"https://maps.googleapis.com/maps/api/directions/json?origin={origin.Latitude},{origin.Longitude}&destination={destination.Latitude},{destination.Longitude}&key={_apiKey}";

                HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync(cancellationToken);
                GoogleMapsDirectionsResponse? result = JsonSerializer.Deserialize<GoogleMapsDirectionsResponse>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result is { Status: "OK", Routes.Count: > 0 })
                {
                    DirectionsRoute route = result.Routes[0];
                    DirectionsLeg leg = route.Legs[0];

                    return new RouteResult
                    {
                        Distance = leg.Distance.Value * 0.000621371, // Convert meters to miles
                        Duration = TimeSpan.FromSeconds(leg.Duration.Value),
                        Waypoints = [origin, destination],
                        Polyline = route.OverviewPolyline.Points,
                        EstimatedArrival = DateTime.UtcNow.AddSeconds(leg.Duration.Value),
                        IsValid = true
                    };
                }

                return new RouteResult
                {
                    IsValid = false,
                    ErrorMessage = result?.Status ?? "No route found"
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
            // In a real implementation, this would use Google Maps Directions API with waypoints optimization
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

        private static string GetAddressComponent(List<AddressComponent> components, string type)
        {
            AddressComponent? component = components.Find(c => c.Types.Contains(type));
            return component?.LongName ?? string.Empty;
        }

        // Internal classes for Google Maps API responses
        private sealed class GoogleMapsGeocodeResponse
        {
            public string Status { get; init; } = string.Empty;
            public List<GeocodeResult> Results { get; init; } = [];
        }

        private sealed class GeocodeResult
        {
            [JsonPropertyName("formatted_address")]
            public string FormattedAddress { get; init; } = string.Empty;

            public Geometry Geometry { get; init; } = new();
            public List<AddressComponent> AddressComponents { get; init; } = [];
        }

        private sealed class Geometry
        {
            [JsonPropertyName("location")] public Coordinate Location { get; init; } = new();
        }

        private sealed class Coordinate
        {
            [JsonPropertyName("lat")] public decimal Lat { get; init; }

            [JsonPropertyName("lng")] public decimal Lng { get; init; }
        }

        private sealed class AddressComponent
        {
            public string LongName { get; init; } = string.Empty;
            public List<string> Types { get; init; } = [];
        }

        private sealed class GoogleMapsDirectionsResponse
        {
            public string Status { get; init; } = string.Empty;
            public List<DirectionsRoute> Routes { get; init; } = [];
        }

        private sealed class DirectionsRoute
        {
            public List<DirectionsLeg> Legs { get; init; } = [];

            [JsonPropertyName("overview_polyline")]
            public Polyline OverviewPolyline { get; init; } = new();
        }

        private sealed class DirectionsLeg
        {
            public Distance Distance { get; init; } = new();
            public Duration Duration { get; init; } = new();
        }

        private sealed class Distance
        {
            [JsonPropertyName("value")] public int Value { get; init; } // meters
        }

        private sealed class Duration
        {
            [JsonPropertyName("value")] public int Value { get; init; } // seconds
        }

        private sealed class Polyline
        {
            [JsonPropertyName("points")] public string Points { get; init; } = string.Empty;
        }
    }
}
