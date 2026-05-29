using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TripBrain.Models;

namespace TripBrain.Services;

public class TripGeneratorService : ITripGeneratorService
{
    private readonly IGeocodingService _geocodingService;
    private readonly IWeatherService _weatherService;
    private readonly IAttractionService _attractionService;
    private readonly ILogger<TripGeneratorService> _logger;

    public TripGeneratorService(
        IGeocodingService geocodingService,
        IWeatherService weatherService,
        IAttractionService attractionService,
        ILogger<TripGeneratorService> logger)
    {
        _geocodingService = geocodingService;
        _weatherService = weatherService;
        _attractionService = attractionService;
        _logger = logger;
    }

    public async Task<Trip> GenerateTripPlanAsync(string destination, string budget, int days, List<string> interests)
    {
        _logger.LogInformation("Generating trip plan for '{Destination}' ({Days} days, Budget: {Budget})...", destination, days, budget);

        // 1. Geocode Destination
        var geocodeResult = await _geocodingService.GeocodeCityAsync(destination);
        if (geocodeResult == null)
        {
            throw new ArgumentException($"Zielort '{destination}' konnte nicht gefunden werden. Bitte überprüfe die Schreibweise und versuche es erneut.");
        }

        // 2. Fetch Weather
        var weatherForecast = await _weatherService.GetWeatherForecastAsync(geocodeResult.Latitude, geocodeResult.Longitude, days);
        if (weatherForecast == null || weatherForecast.Count == 0)
        {
            _logger.LogWarning("Failed to fetch weather forecast. Creating placeholder weather entries.");
            weatherForecast = CreatePlaceholderWeather(days);
        }

        // 3. Fetch Attractions
        var rawAttractions = await _attractionService.GetAttractionsAsync(
            geocodeResult.Latitude, 
            geocodeResult.Longitude, 
            geocodeResult.ResolvedName, 
            interests
        );

        // 4. Apply Budget Filters
        var attractionsPool = FilterByBudget(rawAttractions, budget);

        // 5. Generate Day-by-Day Itinerary (Weather-Aware & Proximity-Sequenced)
        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DestinationCity = geocodeResult.ResolvedName,
            Latitude = geocodeResult.Latitude,
            Longitude = geocodeResult.Longitude,
            Budget = budget,
            DurationDays = days,
            Interests = interests.ToList(),
            CreatedAt = DateTime.UtcNow
        };

        var dayPlans = new List<DayPlan>();
        var usedAttractionIds = new HashSet<string>();

        for (int i = 0; i < days; i++)
        {
            var dayNum = i + 1;
            // Get weather for the day (wrap around if forecast is shorter than duration)
            var weather = weatherForecast[i % weatherForecast.Count];
            var isRainy = weather.ConditionCode == "rain" || weather.ConditionCode == "snow";

            var dayPlan = new DayPlan
            {
                Id = Guid.NewGuid(),
                TripId = trip.Id,
                DayNumber = dayNum,
                Date = weather.Date,
                WeatherSummary = weather.Summary,
                WeatherTemp = Math.Round(weather.AverageTemp, 1),
                WeatherConditionCode = weather.ConditionCode,
                WeatherIcon = weather.Icon
            };

            // Build Day activities (Morning, Lunch/Midday, Afternoon/Evening)
            var activities = GenerateDayActivities(dayPlan.Id, attractionsPool, usedAttractionIds, isRainy, geocodeResult.Latitude, geocodeResult.Longitude);
            dayPlan.Activities = activities;
            dayPlans.Add(dayPlan);
        }

        trip.DayPlans = dayPlans;
        trip.TotalEstimatedCost = dayPlans.Sum(dp => dp.Activities.Sum(a => a.EstimatedCost));

        return trip;
    }

    private List<AttractionResult> FilterByBudget(List<AttractionResult> attractions, string budget)
    {
        var filtered = budget.ToLowerInvariant() switch
        {
            "economy" => attractions.Where(a => a.EstimatedCost <= 25m).ToList(),
            "mid-range" => attractions.Where(a => a.EstimatedCost <= 55m).ToList(),
            "luxury" => attractions.OrderByDescending(a => a.Rating).ThenByDescending(a => a.EstimatedCost).ToList(),
            _ => attractions
        };

        // Safety net: if filtered pool is too small, fallback to all attractions to avoid empty itineraries
        if (filtered.Count < 5)
        {
            return attractions;
        }

        return filtered;
    }

    private List<DayActivity> GenerateDayActivities(
        Guid dayPlanId, 
        List<AttractionResult> pool, 
        HashSet<string> usedNames, 
        bool isRainy,
        double centerLat,
        double centerLon)
    {
        var dayActivities = new List<DayActivity>();
        
        // If we are running low on fresh attractions, clear the used names to allow reuse
        if (pool.Count(a => !usedNames.Contains(a.Name)) < 4)
        {
            usedNames.Clear();
        }
        
        var diningPool = pool.Where(a => a.Category == "Food" && !usedNames.Contains(a.Name)).ToList();
        var indoorPool = pool.Where(a => a.IsIndoor && a.Category != "Food" && !usedNames.Contains(a.Name)).ToList();
        var outdoorPool = pool.Where(a => !a.IsIndoor && a.Category != "Food" && !usedNames.Contains(a.Name)).ToList();
        var generalPool = pool.Where(a => !usedNames.Contains(a.Name)).ToList();

        // Fallbacks if pools are empty due to usedNames exhaustion
        if (isRainy && !indoorPool.Any()) indoorPool = pool.Where(a => a.IsIndoor && a.Category != "Food").ToList();
        if (!isRainy && !outdoorPool.Any()) outdoorPool = pool.Where(a => !a.IsIndoor && a.Category != "Food").ToList();
        if (!diningPool.Any()) diningPool = pool.Where(a => a.Category == "Food").ToList();
        if (!generalPool.Any()) generalPool = pool.ToList();

        // 1. SELECT MORNING ACTIVITY (Approx 9:00 AM)
        AttractionResult? morningSpot = null;
        if (isRainy && indoorPool.Any())
        {
            morningSpot = indoorPool.OrderByDescending(a => a.Rating).First();
        }
        else if (!isRainy && outdoorPool.Any())
        {
            morningSpot = outdoorPool.OrderByDescending(a => a.Rating).First();
        }
        else if (generalPool.Any())
        {
            // If rainy but no indoor at all, try to find something indoor anyway, else generic
            morningSpot = generalPool.OrderByDescending(a => isRainy && a.IsIndoor ? 1 : 0).ThenByDescending(a => a.Rating).First();
        }

        if (morningSpot == null)
        {
            morningSpot = CreateFallbackAttraction(isRainy, "Morning", centerLat, centerLon);
        }

        usedNames.Add(morningSpot.Name);
        dayActivities.Add(MapToDayActivity(dayPlanId, morningSpot, "Vormittag", 9.0, 2.5));

        // 2. SELECT LUNCH SPOT (Approx 12:30 PM)
        var refLat = morningSpot.Latitude;
        var refLon = morningSpot.Longitude;

        AttractionResult? lunchSpot = null;
        if (diningPool.Any())
        {
            lunchSpot = diningPool.OrderBy(a => CalculateDistance(refLat, refLon, a.Latitude, a.Longitude)).First();
        }
        else
        {
            lunchSpot = generalPool
                .Where(a => a.Category == "Food" || a.Name.Contains("Food") || a.Name.Contains("Bistro") || a.Name.Contains("Market") || a.Name.Contains("Restaurant") || a.Name.Contains("Cafe"))
                .OrderBy(a => CalculateDistance(refLat, refLon, a.Latitude, a.Longitude))
                .FirstOrDefault();
        }

        if (lunchSpot == null)
        {
            lunchSpot = CreateFallbackAttraction(false, "Lunch", refLat, refLon, true);
        }

        usedNames.Add(lunchSpot.Name);
        dayActivities.Add(MapToDayActivity(dayPlanId, lunchSpot, "Mittagessen", 12.5, 1.5));

        // 3. SELECT AFTERNOON ACTIVITY (Approx 2:30 PM)
        refLat = lunchSpot.Latitude;
        refLon = lunchSpot.Longitude;

        indoorPool = indoorPool.Where(a => !usedNames.Contains(a.Name)).ToList();
        outdoorPool = outdoorPool.Where(a => !usedNames.Contains(a.Name)).ToList();
        generalPool = generalPool.Where(a => !usedNames.Contains(a.Name)).ToList();

        // Fallbacks again for afternoon
        if (isRainy && !indoorPool.Any()) indoorPool = pool.Where(a => a.IsIndoor && a.Category != "Food" && !usedNames.Contains(a.Name)).ToList();
        if (!isRainy && !outdoorPool.Any()) outdoorPool = pool.Where(a => !a.IsIndoor && a.Category != "Food" && !usedNames.Contains(a.Name)).ToList();
        if (!generalPool.Any()) generalPool = pool.Where(a => !usedNames.Contains(a.Name)).ToList();

        AttractionResult? afternoonSpot = null;
        if (isRainy && indoorPool.Any())
        {
            afternoonSpot = indoorPool.OrderBy(a => CalculateDistance(refLat, refLon, a.Latitude, a.Longitude)).First();
        }
        else if (!isRainy && outdoorPool.Any())
        {
            afternoonSpot = outdoorPool.OrderBy(a => CalculateDistance(refLat, refLon, a.Latitude, a.Longitude)).First();
        }
        else if (generalPool.Any(a => a.Category != "Food"))
        {
            afternoonSpot = generalPool.Where(a => a.Category != "Food").OrderBy(a => CalculateDistance(refLat, refLon, a.Latitude, a.Longitude)).First();
        }
        else if (generalPool.Any())
        {
            afternoonSpot = generalPool.OrderBy(a => CalculateDistance(refLat, refLon, a.Latitude, a.Longitude)).First();
        }

        if (afternoonSpot == null)
        {
            afternoonSpot = CreateFallbackAttraction(isRainy, "Afternoon", refLat, refLon);
        }

        usedNames.Add(afternoonSpot.Name);
        dayActivities.Add(MapToDayActivity(dayPlanId, afternoonSpot, "Nachmittag", 14.5, 3.0));

        // 4. SELECT EVENING EXPERIENCE (Approx 6:30 PM)
        refLat = afternoonSpot.Latitude;
        refLon = afternoonSpot.Longitude;
        generalPool = generalPool.Where(a => !usedNames.Contains(a.Name)).ToList();
        
        if (!generalPool.Any()) generalPool = pool.Where(a => !usedNames.Contains(a.Name)).ToList();

        AttractionResult? eveningSpot = null;
        if (generalPool.Any())
        {
            eveningSpot = generalPool.OrderBy(a => CalculateDistance(refLat, refLon, a.Latitude, a.Longitude)).First();
        }

        if (eveningSpot == null)
        {
            eveningSpot = CreateFallbackAttraction(false, "Evening", refLat, refLon);
        }

        usedNames.Add(eveningSpot.Name);
        dayActivities.Add(MapToDayActivity(dayPlanId, eveningSpot, "Abend", 18.5, 2.0));

        return dayActivities;
    }

    private AttractionResult CreateFallbackAttraction(bool isRainy, string timeOfDay, double lat, double lon, bool isFood = false)
    {
        if (isFood)
        {
            return new AttractionResult
            {
                Name = "Lokales Restaurant",
                Description = "Ein hoch bewertetes lokales Restaurant oder Café für eine entspannte Mahlzeit.",
                Category = "Food",
                IsIndoor = true,
                Latitude = lat,
                Longitude = lon,
                Rating = 4.5,
                EstimatedCost = 25m,
                DurationHours = 1.5,
                Address = "Stadtzentrum"
            };
        }

        if (isRainy)
        {
            return new AttractionResult
            {
                Name = timeOfDay == "Morning" ? "Stadtgeschichtliches Museum" : "Lokale Kunstgalerie & Einkaufen",
                Description = "Eine großartige Indoor-Aktivität, um dem Regen zu entkommen und die lokale Kultur zu erkunden.",
                Category = "Culture",
                IsIndoor = true,
                Latitude = lat,
                Longitude = lon,
                Rating = 4.5,
                EstimatedCost = 15m,
                DurationHours = 2.5,
                Address = "Stadtzentrum"
            };
        }
        else
        {
            return new AttractionResult
            {
                Name = timeOfDay == "Morning" ? "Stadtpark & Botanischer Garten" : "Historischer Stadtrundgang",
                Description = "Genieße das schöne Wetter und erkunde die Umgebung.",
                Category = "Sightseeing",
                IsIndoor = false,
                Latitude = lat,
                Longitude = lon,
                Rating = 4.5,
                EstimatedCost = 0m,
                DurationHours = 2.5,
                Address = "Stadtzentrum"
            };
        }
    }

    private DayActivity MapToDayActivity(Guid dayPlanId, AttractionResult att, string recTime, double startHour, double duration)
    {
        return new DayActivity
        {
            Id = Guid.NewGuid(),
            DayPlanId = dayPlanId,
            Name = att.Name,
            Description = att.Description,
            Category = att.Category,
            EstimatedCost = att.EstimatedCost,
            DurationHours = duration > 0 ? duration : att.DurationHours,
            RecommendedTime = $"{recTime} ({FormatHour(startHour)})",
            Latitude = att.Latitude,
            Longitude = att.Longitude,
            IsIndoor = att.IsIndoor,
            Address = att.Address,
            Rating = att.Rating
        };
    }

    private string FormatHour(double hour)
    {
        int h = (int)Math.Floor(hour);
        int m = (int)((hour - h) * 60);
        return $"{h:D2}:{m:D2}";
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Simple Euclidean distance for local calculations
        double dLat = lat1 - lat2;
        double dLon = lon1 - lon2;
        return Math.Sqrt(dLat * dLat + dLon * dLon);
    }

    private List<WeatherData> CreatePlaceholderWeather(int days)
    {
        var list = new List<WeatherData>();
        var today = DateTime.Today;
        for (int i = 0; i < days; i++)
        {
            list.Add(new WeatherData
            {
                Date = today.AddDays(i),
                TempMax = 22.0,
                TempMin = 14.0,
                Summary = "Mild und Klar",
                ConditionCode = "clear",
                Icon = "sun"
            });
        }
        return list;
    }
}
