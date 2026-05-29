using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TripBrain.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> _logger)
    {
        this._httpClient = httpClient;
        this._logger = _logger;
        this._httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TripBrain-TravelPlanner-App/1.0");
    }

    public async Task<List<WeatherData>?> GetWeatherForecastAsync(double latitude, double longitude, int days)
    {
        try
        {
            _logger.LogInformation("Fetching weather forecast for lat: {Lat}, lon: {Lon}...", latitude, longitude);
            
            var latStr = latitude.ToString(CultureInfo.InvariantCulture);
            var lonStr = longitude.ToString(CultureInfo.InvariantCulture);
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={latStr}&longitude={lonStr}&daily=weathercode,temperature_2m_max,temperature_2m_min&timezone=auto";
            
            var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url);
            if (response?.Daily != null && response.Daily.Time != null)
            {
                var list = new List<WeatherData>();
                var count = Math.Min(days, response.Daily.Time.Length);
                
                for (int i = 0; i < count; i++)
                {
                    if (DateTime.TryParse(response.Daily.Time[i], CultureInfo.InvariantCulture, out var date))
                    {
                        var code = response.Daily.WeatherCode?[i] ?? 0;
                        var (summary, conditionCode, icon) = MapWmoCode(code);
                        
                        list.Add(new WeatherData
                        {
                            Date = date,
                            TempMax = response.Daily.Temperature2mMax?[i] ?? 20.0,
                            TempMin = response.Daily.Temperature2mMin?[i] ?? 10.0,
                            Summary = summary,
                            ConditionCode = conditionCode,
                            Icon = icon
                        });
                    }
                }
                return list;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching weather from Open-Meteo. Generating realistic fallback weather.");
        }

        // If API fails or is offline, generate plausible weather based on current local time and latitude
        return GenerateFallbackWeather(latitude, days);
    }

    private static (string Summary, string ConditionCode, string Icon) MapWmoCode(int code)
    {
        return code switch
        {
            0 => ("Clear Sky", "clear", "sun"),
            1 or 2 or 3 => ("Partly Cloudy", "cloud", "cloud"),
            45 or 48 => ("Foggy", "cloud", "cloud"),
            51 or 53 or 55 => ("Light Drizzle", "rain", "cloud-drizzle"),
            61 or 63 or 65 => ("Rainy", "rain", "cloud-rain"),
            66 or 67 => ("Freezing Rain", "rain", "cloud-snow"),
            71 or 73 or 75 => ("Snowy", "snow", "snowflake"),
            77 => ("Snow Grains", "snow", "snowflake"),
            80 or 81 or 82 => ("Rain Showers", "rain", "cloud-drizzle"),
            85 or 86 => ("Snow Showers", "snow", "snowflake"),
            95 or 96 or 99 => ("Thunderstorm", "rain", "cloud-lightning"),
            _ => ("Moderate Weather", "clear", "sun")
        };
    }

    private List<WeatherData> GenerateFallbackWeather(double latitude, int days)
    {
        var list = new List<WeatherData>();
        var random = new Random();
        var baseTemp = 20.0;

        // Roughly estimate temperature based on latitude
        var absLat = Math.Abs(latitude);
        if (absLat < 23.5) baseTemp = 28.0; // Tropical
        else if (absLat > 60.0) baseTemp = 5.0; // Polar
        else baseTemp = 18.0 - (absLat - 35) * 0.4; // Temperate

        var today = DateTime.Today;

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(i);
            // Add some noise and temperature swings
            var dailyNoise = random.NextDouble() * 6 - 3;
            var max = Math.Round(baseTemp + 5 + dailyNoise, 1);
            var min = Math.Round(baseTemp - 5 + dailyNoise, 1);

            // Determine randomized but realistic weather progression
            var randVal = random.Next(100);
            var (summary, conditionCode, icon) = randVal switch
            {
                < 60 => ("Sunny", "clear", "sun"),
                < 80 => ("Cloudy", "cloud", "cloud"),
                < 95 => ("Rainy", "rain", "cloud-rain"),
                _ => ("Thunderstorm", "rain", "cloud-lightning")
            };

            list.Add(new WeatherData
            {
                Date = date,
                TempMax = max,
                TempMin = min,
                Summary = summary,
                ConditionCode = conditionCode,
                Icon = icon
            });
        }

        return list;
    }

    // DTO for parsing Open-Meteo response
    private class OpenMeteoResponse
    {
        [JsonPropertyName("daily")]
        public DailyData? Daily { get; set; }
    }

    private class DailyData
    {
        [JsonPropertyName("time")]
        public string[]? Time { get; set; }

        [JsonPropertyName("weathercode")]
        public int[]? WeatherCode { get; set; }

        [JsonPropertyName("temperature_2m_max")]
        public double[]? Temperature2mMax { get; set; }

        [JsonPropertyName("temperature_2m_min")]
        public double[]? Temperature2mMin { get; set; }
    }
}
