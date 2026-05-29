using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TripBrain.Services;

public class WeatherData
{
    public DateTime Date { get; set; }
    public double TempMax { get; set; }
    public double TempMin { get; set; }
    public double AverageTemp => (TempMax + TempMin) / 2.0;
    public string Summary { get; set; } = "Sunny";
    public string ConditionCode { get; set; } = "clear"; // clear, rain, cloud, snow
    public string Icon { get; set; } = "sun";
}

public interface IWeatherService
{
    Task<List<WeatherData>?> GetWeatherForecastAsync(double latitude, double longitude, int days);
}
