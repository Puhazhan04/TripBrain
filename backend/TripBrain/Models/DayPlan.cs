using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TripBrain.Models;

public class DayPlan
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public int DayNumber { get; set; }
    public DateTime Date { get; set; }
    public string WeatherSummary { get; set; } = "Sunny";
    public double WeatherTemp { get; set; }
    public string WeatherConditionCode { get; set; } = "clear"; // clear, rain, cloud, snow
    public string WeatherIcon { get; set; } = "sun";
    
    // Navigation properties
    [JsonIgnore]
    public Trip? Trip { get; set; }
    
    public List<DayActivity> Activities { get; set; } = new();
}
