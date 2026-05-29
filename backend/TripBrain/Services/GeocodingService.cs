using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TripBrain.Services;

public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeocodingService> _logger;

    public GeocodingService(HttpClient httpClient, IConfiguration configuration, ILogger<GeocodingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        
        // Nominatim OSM and Open-Meteo require a user agent header
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TripBrain-TravelPlanner-App/1.0");
    }

    public async Task<GeocodingResult?> GeocodeCityAsync(string cityName)
    {
        if (string.IsNullOrWhiteSpace(cityName)) return null;

        var apiKey = _configuration["Geoapify:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            return await GeocodeWithGeoapifyAsync(cityName, apiKey);
        }

        return await GeocodeWithNominatimAsync(cityName);
    }

    private async Task<GeocodingResult?> GeocodeWithGeoapifyAsync(string cityName, string apiKey)
    {
        try
        {
            _logger.LogInformation("Geocoding '{City}' using Geoapify API...", cityName);
            var url = $"https://api.geoapify.com/v1/geocode/search?text={Uri.EscapeDataString(cityName)}&limit=1&apiKey={apiKey}";
            var response = await _httpClient.GetFromJsonAsync<GeoapifyGeocodeResponse>(url);

            if (response?.Features != null && response.Features.Length > 0)
            {
                var feature = response.Features[0];
                // Geoapify coordinates are [longitude, latitude]
                if (feature.Geometry?.Coordinates != null && feature.Geometry.Coordinates.Length >= 2)
                {
                    return new GeocodingResult
                    {
                        Longitude = feature.Geometry.Coordinates[0],
                        Latitude = feature.Geometry.Coordinates[1],
                        ResolvedName = feature.Properties?.Formatted ?? cityName
                    };
                }
            }
            _logger.LogWarning("Geoapify returned empty geocoding results for '{City}'", cityName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding with Geoapify for '{City}'. Falling back to Nominatim.", cityName);
        }

        // Fallback to Nominatim if Geoapify fails
        return await GeocodeWithNominatimAsync(cityName);
    }

    private async Task<GeocodingResult?> GeocodeWithNominatimAsync(string cityName)
    {
        try
        {
            _logger.LogInformation("Geocoding '{City}' using OSM Nominatim API...", cityName);
            var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(cityName)}&format=json&limit=1";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OSM Nominatim returned status code {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var first = root[0];
                var latStr = first.GetProperty("lat").GetString();
                var lonStr = first.GetProperty("lon").GetString();
                var displayName = first.GetProperty("display_name").GetString() ?? cityName;

                if (double.TryParse(latStr, CultureInfo.InvariantCulture, out double lat) &&
                    double.TryParse(lonStr, CultureInfo.InvariantCulture, out double lon))
                {
                    return new GeocodingResult
                    {
                        Latitude = lat,
                        Longitude = lon,
                        ResolvedName = displayName
                    };
                }
            }
            _logger.LogWarning("OSM Nominatim returned empty results for '{City}'", cityName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding with OSM Nominatim for '{City}'", cityName);
        }

        return null;
    }

    // JSON DTOs for Geoapify Geocoding
    private class GeoapifyGeocodeResponse
    {
        [JsonPropertyName("features")]
        public GeoapifyFeature[] Features { get; set; } = Array.Empty<GeoapifyFeature>();
    }

    private class GeoapifyFeature
    {
        [JsonPropertyName("geometry")]
        public GeoapifyGeometry? Geometry { get; set; }
        
        [JsonPropertyName("properties")]
        public GeoapifyProperties? Properties { get; set; }
    }

    private class GeoapifyGeometry
    {
        [JsonPropertyName("coordinates")]
        public double[] Coordinates { get; set; } = Array.Empty<double>();
    }

    private class GeoapifyProperties
    {
        [JsonPropertyName("formatted")]
        public string Formatted { get; set; } = string.Empty;
    }
}
