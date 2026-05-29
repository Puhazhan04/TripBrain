using System;
using System.Collections.Generic;

namespace TripBrain.Models;

public class Trip
{
    public Guid Id { get; set; }
    public string DestinationCity { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Budget { get; set; } = "Mid-range"; // Economy, Mid-range, Luxury
    public int DurationDays { get; set; }
    public List<string> Interests { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal TotalEstimatedCost { get; set; }
    
    // Navigation property
    public List<DayPlan> DayPlans { get; set; } = new();
}
