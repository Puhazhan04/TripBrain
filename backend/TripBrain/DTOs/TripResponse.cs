using System;
using System.Collections.Generic;

namespace TripBrain.DTOs;

public class DayActivityResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public double DurationHours { get; set; }
    public string RecommendedTime { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsIndoor { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Rating { get; set; }
}

public class DayPlanResponse
{
    public Guid Id { get; set; }
    public int DayNumber { get; set; }
    public DateTime Date { get; set; }
    public string WeatherSummary { get; set; } = string.Empty;
    public double WeatherTemp { get; set; }
    public string WeatherConditionCode { get; set; } = string.Empty;
    public string WeatherIcon { get; set; } = string.Empty;
    public List<DayActivityResponse> Activities { get; set; } = new();
}

public class TripResponse
{
    public Guid Id { get; set; }
    public string DestinationCity { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Budget { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public List<string> Interests { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public List<DayPlanResponse> DayPlans { get; set; } = new();
}
