using System;
using System.Text.Json.Serialization;

namespace TripBrain.Models;

public class DayActivity
{
    public Guid Id { get; set; }
    public Guid DayPlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Nature, Food, Museum, Adventure, Shopping
    public decimal EstimatedCost { get; set; }
    public double DurationHours { get; set; }
    public string RecommendedTime { get; set; } = "Morning"; // Morning, Afternoon, Evening
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsIndoor { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Rating { get; set; } = 4.0;
    
    // Navigation property
    [JsonIgnore]
    public DayPlan? DayPlan { get; set; }
}
