using System.Threading.Tasks;

namespace TripBrain.Services;

public class GeocodingResult
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ResolvedName { get; set; } = string.Empty;
}

public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeCityAsync(string cityName);
}
