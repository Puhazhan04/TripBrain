using System.Collections.Generic;
using System.Threading.Tasks;

namespace TripBrain.Services;

public class AttractionResult
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Nature, Food, Museum, Adventure, Shopping
    public decimal EstimatedCost { get; set; }
    public double DurationHours { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsIndoor { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Rating { get; set; } = 4.0;
}

public interface IAttractionService
{
    Task<List<AttractionResult>> GetAttractionsAsync(double latitude, double longitude, string cityName, List<string> interests);
}
