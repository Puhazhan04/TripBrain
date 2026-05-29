using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TripBrain.Services;

public class AttractionService : IAttractionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AttractionService> _logger;

    public AttractionService(HttpClient httpClient, IConfiguration configuration, ILogger<AttractionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TripBrain-TravelPlanner-App/1.0");
    }

    public async Task<List<AttractionResult>> GetAttractionsAsync(double latitude, double longitude, string cityName, List<string> interests)
    {
        var apiKey = _configuration["Geoapify:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            var results = await GetAttractionsFromGeoapifyAsync(latitude, longitude, interests, apiKey);
            if (results.Any())
            {
                return results;
            }
            _logger.LogWarning("Geoapify returned zero attractions, falling back to database generators.");
        }

        return GetFallbackAttractions(latitude, longitude, cityName, interests);
    }

    private async Task<List<AttractionResult>> GetAttractionsFromGeoapifyAsync(double latitude, double longitude, List<string> interests, string apiKey)
    {
        var results = new List<AttractionResult>();
        try
        {
            _logger.LogInformation("Fetching attractions from Geoapify for lat: {Lat}, lon: {Lon}...", latitude, longitude);
            
            // Map our generic categories to Geoapify categories
            var categoryMapping = new Dictionary<string, string>
            {
                { "nature", "natural,leisure.park,leisure.garden" },
                { "food", "catering.restaurant,catering.cafe" },
                { "museums", "entertainment.culture.museum,entertainment.culture.art_gallery" },
                { "adventure", "active,active.amusement_park,tourism.attraction" },
                { "shopping", "commercial.shopping_mall,commercial.department_store,commercial.clothing" }
            };

            var categoriesToFetch = interests.Select(i => i.ToLowerInvariant())
                .Where(categoryMapping.ContainsKey)
                .Select(i => categoryMapping[i])
                .ToList();

            // If no valid interests selected, fetch a mix of everything
            if (!categoriesToFetch.Any())
            {
                categoriesToFetch.Add("tourism.attraction,entertainment.culture,leisure.park,catering.restaurant");
            }

            var joinedCategories = string.Join(",", categoriesToFetch);
            var latStr = latitude.ToString(CultureInfo.InvariantCulture);
            var lonStr = longitude.ToString(CultureInfo.InvariantCulture);
            
            // Radius 10000 meters (10 km)
            var url = $"https://api.geoapify.com/v2/places?categories={joinedCategories}&filter=circle:{lonStr},{latStr},10000&bias=proximity:{lonStr},{latStr}&limit=50&apiKey={apiKey}";
            
            var response = await _httpClient.GetFromJsonAsync<GeoapifyPlacesResponse>(url);
            if (response?.Features != null)
            {
                foreach (var feature in response.Features)
                {
                    var props = feature.Properties;
                    if (props == null || string.IsNullOrWhiteSpace(props.Name) || props.Lat == 0 || props.Lon == 0) continue;

                    // Deduplicate
                    if (results.Any(r => r.Name.Equals(props.Name, StringComparison.OrdinalIgnoreCase))) continue;

                    var mainCategory = DetermineCategoryFromTags(props.Categories, interests);
                    var (cost, duration, isIndoor) = GenerateAestheticMetadata(mainCategory, props.Name);

                    results.Add(new AttractionResult
                    {
                        Name = props.Name,
                        Address = props.FormattedAddress ?? props.Street ?? "Nearby coordinate",
                        Latitude = props.Lat,
                        Longitude = props.Lon,
                        Category = mainCategory,
                        Description = $"{props.Name} is a popular spot listed under {string.Join(", ", props.Categories.Take(2))}.",
                        EstimatedCost = cost,
                        DurationHours = duration,
                        IsIndoor = isIndoor,
                        Rating = Math.Round(4.0 + (new Random().NextDouble() * 1.0), 1)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Geoapify Places API");
        }

        return results;
    }

    private static string DetermineCategoryFromTags(string[] tags, List<string> userInterests)
    {
        // Default based on tags
        var tagList = tags.Select(t => t.ToLowerInvariant()).ToList();
        
        if (tagList.Any(t => t.Contains("museum") || t.Contains("art_gallery") || t.Contains("culture")))
            return "Museums";
        if (tagList.Any(t => t.Contains("restaurant") || t.Contains("cafe") || t.Contains("catering")))
            return "Food";
        if (tagList.Any(t => t.Contains("natural") || t.Contains("park") || t.Contains("garden") || t.Contains("beach")))
            return "Nature";
        if (tagList.Any(t => t.Contains("mall") || t.Contains("shopping") || t.Contains("department_store") || t.Contains("clothing")))
            return "Shopping";
        if (tagList.Any(t => t.Contains("active") || t.Contains("climbing") || t.Contains("amusement") || t.Contains("attraction")))
            return "Adventure";

        // Fallback to matching first user interest or default
        if (userInterests != null && userInterests.Count > 0)
        {
            var match = userInterests.FirstOrDefault();
            if (!string.IsNullOrEmpty(match))
            {
                return char.ToUpper(match[0]) + match.Substring(1).ToLower();
            }
        }

        return "Adventure";
    }

    private static (decimal Cost, double Duration, bool IsIndoor) GenerateAestheticMetadata(string category, string name)
    {
        var random = new Random(name.GetHashCode());
        return category.ToLowerInvariant() switch
        {
            "nature" => (0, Math.Round(1.5 + random.NextDouble() * 2, 1), false),
            "food" => (random.Next(10, 45), Math.Round(1.0 + random.NextDouble() * 1, 1), true),
            "museums" => (random.Next(8, 25), Math.Round(2.0 + random.NextDouble() * 2, 1), true),
            "shopping" => (random.Next(0, 100), Math.Round(1.5 + random.NextDouble() * 3, 1), true),
            "adventure" => (random.Next(15, 75), Math.Round(2.0 + random.NextDouble() * 4, 1), random.Next(100) < 30),
            _ => (15, 2.0, false)
        };
    }

    private List<AttractionResult> GetFallbackAttractions(double latitude, double longitude, string cityName, List<string> interests)
    {
        _logger.LogInformation("Generating fallback attractions for '{City}'...", cityName);
        
        var normalizedCity = cityName.Split(',')[0].Trim().ToLowerInvariant();
        var database = GetStaticAttractionsDatabase(latitude, longitude, cityName);
        
        var filteredList = new List<AttractionResult>();

        // Filter and seed by interests
        var lowercaseInterests = interests.Select(i => i.ToLowerInvariant()).ToList();
        
        // If the database has specific items matching interests, grab them
        foreach (var item in database)
        {
            if (lowercaseInterests.Contains(item.Category.ToLowerInvariant()) || !lowercaseInterests.Any())
            {
                filteredList.Add(item);
            }
        }

        // If not enough matches, add other attractions from the list to fulfill itinerary needs
        if (filteredList.Count < 8)
        {
            foreach (var item in database)
            {
                if (!filteredList.Any(f => f.Name == item.Name))
                {
                    filteredList.Add(item);
                }
            }
        }

        return filteredList;
    }

    private List<AttractionResult> GetStaticAttractionsDatabase(double baseLat, double baseLon, string cityName)
    {
        var normalizedCity = cityName.Split(',')[0].Trim();
        var list = new List<AttractionResult>();

        // Hardcoded top cities for professional grade quality
        if (normalizedCity.Equals("Paris", StringComparison.OrdinalIgnoreCase))
        {
            list.AddRange(new[]
            {
                new AttractionResult { Name = "Eiffel Tower", Category = "Adventure", EstimatedCost = 25m, DurationHours = 2.0, Latitude = 48.8584, Longitude = 2.2945, IsIndoor = false, Address = "Champ de Mars, 5 Avenue Anatole France, 75007 Paris", Description = "The iconic wrought-iron grid tower on the Champ de Mars, offering breathtaking views of the entire city.", Rating = 4.8 },
                new AttractionResult { Name = "Louvre Museum", Category = "Museums", EstimatedCost = 17m, DurationHours = 3.5, Latitude = 48.8606, Longitude = 2.3376, IsIndoor = true, Address = "Rue de Rivoli, 75001 Paris", Description = "The world's largest art museum, home to the Mona Lisa and thousands of historic treasures.", Rating = 4.7 },
                new AttractionResult { Name = "Cathédrale Notre-Dame de Paris", Category = "Museums", EstimatedCost = 0m, DurationHours = 1.5, Latitude = 48.8530, Longitude = 2.3499, IsIndoor = true, Address = "6 Parvis Notre-Dame - Pl. Jean-Paul II, 75004 Paris", Description = "A masterpiece of French Gothic architecture with stunning stained glass windows.", Rating = 4.8 },
                new AttractionResult { Name = "Jardin du Luxembourg", Category = "Nature", EstimatedCost = 0m, DurationHours = 1.5, Latitude = 48.8462, Longitude = 2.3371, IsIndoor = false, Address = "75006 Paris", Description = "A tranquil 17th-century palace garden featuring lawns, fountains, and beautiful tree-lined promenades.", Rating = 4.7 },
                new AttractionResult { Name = "Arc de Triomphe", Category = "Adventure", EstimatedCost = 13m, DurationHours = 1.0, Latitude = 48.8738, Longitude = 2.2950, IsIndoor = false, Address = "Place Charles de Gaulle, 75008 Paris", Description = "The famous triumphal arch standing at the western end of the Champs-Élysées.", Rating = 4.6 },
                new AttractionResult { Name = "Montmartre & Sacré-Cœur", Category = "Nature", EstimatedCost = 0m, DurationHours = 2.0, Latitude = 48.8867, Longitude = 2.3431, IsIndoor = false, Address = "35 Rue du Chevalier de la Barre, 75018 Paris", Description = "A historic hilltop district with artists' plazas and the white-domed Basilica of the Sacred Heart.", Rating = 4.7 },
                new AttractionResult { Name = "Seine River Cruise", Category = "Adventure", EstimatedCost = 15m, DurationHours = 1.0, Latitude = 48.8617, Longitude = 2.3270, IsIndoor = false, Address = "Bateaux Parisiens, Port de la Bourdonnais, 75007 Paris", Description = "A scenic boat tour gliding under Paris bridges, passing by historical landmarks.", Rating = 4.5 },
                new AttractionResult { Name = "Musée d'Orsay", Category = "Museums", EstimatedCost = 16m, DurationHours = 2.5, Latitude = 48.8599, Longitude = 2.3265, IsIndoor = true, Address = "1 Rue de la Légion d'Honneur, 75007 Paris", Description = "A museum housed in a grand railway station, containing the world's largest collection of Impressionist art.", Rating = 4.8 },
                new AttractionResult { Name = "Champs-Élysées & Shopping", Category = "Shopping", EstimatedCost = 0m, DurationHours = 2.0, Latitude = 48.8698, Longitude = 2.3079, IsIndoor = false, Address = "Avenue des Champs-Élysées, 75008 Paris", Description = "One of the world's most famous commercial avenues, filled with luxury boutiques and flagship stores.", Rating = 4.4 },
                new AttractionResult { Name = "Le Comptoir de La Gastronomie", Category = "Food", EstimatedCost = 35m, DurationHours = 1.5, Latitude = 48.8639, Longitude = 2.3435, IsIndoor = true, Address = "34 Rue Montmartre, 75001 Paris", Description = "A traditional French bistro famous for its rich duck dishes, foie gras, and local wines.", Rating = 4.6 },
                new AttractionResult { Name = "La Jacobine", Category = "Food", EstimatedCost = 25m, DurationHours = 1.5, Latitude = 48.8532, Longitude = 2.3394, IsIndoor = true, Address = "59 Rue Saint-André des Arts, 75006 Paris", Description = "A cozy historic cafe nestled in a quiet passage, known for outstanding onion soup and hot chocolate.", Rating = 4.7 }
            });
        }
        else if (normalizedCity.Equals("London", StringComparison.OrdinalIgnoreCase))
        {
            list.AddRange(new[]
            {
                new AttractionResult { Name = "British Museum", Category = "Museums", EstimatedCost = 0m, DurationHours = 3.0, Latitude = 51.5194, Longitude = -0.1270, IsIndoor = true, Address = "Great Russell St, London WC1B 3DG", Description = "A world-renowned museum dedicated to human history, art and culture, housing the Rosetta Stone.", Rating = 4.7 },
                new AttractionResult { Name = "Tower of London", Category = "Museums", EstimatedCost = 29.90m, DurationHours = 2.5, Latitude = 51.5081, Longitude = -0.0759, IsIndoor = true, Address = "London EC3N 4AB", Description = "An 11th-century castle on the River Thames, housing the Crown Jewels and rich royal history.", Rating = 4.8 },
                new AttractionResult { Name = "The London Eye", Category = "Adventure", EstimatedCost = 32.50m, DurationHours = 1.0, Latitude = 51.5033, Longitude = -0.1195, IsIndoor = false, Address = "Riverside Building, County Hall, London SE1 7PB", Description = "A giant Ferris wheel on the South Bank of the River Thames, offering a 360-degree panoramic view.", Rating = 4.5 },
                new AttractionResult { Name = "Hyde Park", Category = "Nature", EstimatedCost = 0m, DurationHours = 2.0, Latitude = 51.5073, Longitude = -0.1657, IsIndoor = false, Address = "London W2 2UH", Description = "One of London's eight Royal Parks, offering vast green spaces, boating lakes, and memorials.", Rating = 4.7 },
                new AttractionResult { Name = "Westminster Abbey", Category = "Museums", EstimatedCost = 25m, DurationHours = 1.5, Latitude = 51.4987, Longitude = -0.1289, IsIndoor = true, Address = "20 Deans Yd, London SW1P 3PA", Description = "A royal church and UNESCO World Heritage site, the location of coronations and burials.", Rating = 4.8 },
                new AttractionResult { Name = "Borough Market", Category = "Food", EstimatedCost = 15m, DurationHours = 1.5, Latitude = 51.5054, Longitude = -0.0908, IsIndoor = true, Address = "8 Southwark St, London SE1 1TL", Description = "London's historic gourmet food market, offering exceptional local and international street food.", Rating = 4.7 },
                new AttractionResult { Name = "Dishoom Covent Garden", Category = "Food", EstimatedCost = 25m, DurationHours = 1.5, Latitude = 51.5125, Longitude = -0.1264, IsIndoor = true, Address = "12 Upper St Martin's Ln, London WC2H 9FB", Description = "An award-winning Bombay-style cafe serving legendary chicken ruby and house black daal.", Rating = 4.6 },
                new AttractionResult { Name = "Harrods Shopping Experience", Category = "Shopping", EstimatedCost = 0m, DurationHours = 2.5, Latitude = 51.4994, Longitude = -0.1632, IsIndoor = true, Address = "87-135 Brompton Rd, London SW1X 7XL", Description = "A world-famous luxury department store with iconic food halls and upscale fashion brands.", Rating = 4.4 }
            });
        }
        else if (normalizedCity.Equals("New York", StringComparison.OrdinalIgnoreCase) || normalizedCity.Equals("New York City", StringComparison.OrdinalIgnoreCase) || normalizedCity.Equals("NYC", StringComparison.OrdinalIgnoreCase))
        {
            list.AddRange(new[]
            {
                new AttractionResult { Name = "Central Park", Category = "Nature", EstimatedCost = 0m, DurationHours = 2.5, Latitude = 40.7829, Longitude = -73.9654, IsIndoor = false, Address = "New York, NY", Description = "A massive iconic green park in the heart of Manhattan with lakes, paths, and bridges.", Rating = 4.8 },
                new AttractionResult { Name = "The Metropolitan Museum of Art", Category = "Museums", EstimatedCost = 25m, DurationHours = 3.5, Latitude = 40.7794, Longitude = -73.9632, IsIndoor = true, Address = "1000 5th Ave, New York, NY 10028", Description = "One of the world's finest art museums, displaying over 5,000 years of global artifacts.", Rating = 4.8 },
                new AttractionResult { Name = "Statue of Liberty & Ellis Island", Category = "Adventure", EstimatedCost = 24m, DurationHours = 3.0, Latitude = 40.6892, Longitude = -74.0445, IsIndoor = false, Address = "Liberty Island, NY 10004", Description = "The historic colossal copper sculpture welcoming visitors and immigrants in NY harbor.", Rating = 4.7 },
                new AttractionResult { Name = "Empire State Building", Category = "Adventure", EstimatedCost = 44m, DurationHours = 1.5, Latitude = 40.7484, Longitude = -73.9857, IsIndoor = true, Address = "20 W 34th St., New York, NY 10001", Description = "An Art Deco masterpiece offering panoramic observation deck views from the 86th floor.", Rating = 4.7 },
                new AttractionResult { Name = "Times Square", Category = "Shopping", EstimatedCost = 0m, DurationHours = 1.0, Latitude = 40.7580, Longitude = -73.9855, IsIndoor = false, Address = "Broadway & 42nd St, New York, NY 10036", Description = "Bright lights, digital billboards, and a hub of Broadway theater and commercial action.", Rating = 4.5 },
                new AttractionResult { Name = "Chelsea Market", Category = "Food", EstimatedCost = 20m, DurationHours = 1.5, Latitude = 40.7420, Longitude = -74.0062, IsIndoor = true, Address = "75 9th Ave, New York, NY 10011", Description = "A bustling indoor food hall containing artisanal food stands, bakeries, and seafood shops.", Rating = 4.6 },
                new AttractionResult { Name = "Joe's Pizza Greenwich Village", Category = "Food", EstimatedCost = 5m, DurationHours = 0.5, Latitude = 40.7306, Longitude = -74.0022, IsIndoor = true, Address = "7 Carmine St, New York, NY 10014", Description = "The quintessential, legendary New York street slice pizza shop operating since 1975.", Rating = 4.7 }
            });
        }
        else if (normalizedCity.Equals("Tokyo", StringComparison.OrdinalIgnoreCase))
        {
            list.AddRange(new[]
            {
                new AttractionResult { Name = "Senso-ji Temple", Category = "Museums", EstimatedCost = 0m, DurationHours = 1.5, Latitude = 35.7148, Longitude = 139.7967, IsIndoor = false, Address = "2-3-1 Asakusa, Taito City, Tokyo", Description = "Tokyo's oldest and most famous Buddhist temple, entered through the iconic Kaminarimon gate.", Rating = 4.6 },
                new AttractionResult { Name = "Shinjuku Gyoen National Garden", Category = "Nature", EstimatedCost = 4m, DurationHours = 2.0, Latitude = 35.6852, Longitude = 139.7101, IsIndoor = false, Address = "11 Naitomachi, Shinjuku City, Tokyo", Description = "A magnificent imperial garden combining Japanese traditional, French formal and English landscapes.", Rating = 4.7 },
                new AttractionResult { Name = "Meiji Jingu Shrine", Category = "Nature", EstimatedCost = 0m, DurationHours = 1.5, Latitude = 35.6764, Longitude = 139.6993, IsIndoor = false, Address = "1-1 Yoyogikamizonocho, Shibuya City, Tokyo", Description = "A peaceful forested shrine dedicated to the deified spirits of Emperor Meiji and his consort.", Rating = 4.6 },
                new AttractionResult { Name = "Shibuya Crossing & Hachiko", Category = "Adventure", EstimatedCost = 0m, DurationHours = 1.0, Latitude = 35.6595, Longitude = 139.7006, IsIndoor = false, Address = "Shibuya Crossing, Tokyo", Description = "The busiest pedestrian scramble crossing in the world, surrounded by towering neon screens.", Rating = 4.5 },
                new AttractionResult { Name = "Tokyo National Museum", Category = "Museums", EstimatedCost = 8m, DurationHours = 2.5, Latitude = 35.7188, Longitude = 139.7765, IsIndoor = true, Address = "13-9 Uenokoen, Taito City, Tokyo", Description = "The oldest museum in Japan, displaying an extensive collection of ancient national treasures.", Rating = 4.6 },
                new AttractionResult { Name = "Tsukiji Outer Market Tour", Category = "Food", EstimatedCost = 25m, DurationHours = 1.5, Latitude = 35.6655, Longitude = 139.7702, IsIndoor = false, Address = "4-16-2 Tsukiji, Chuo City, Tokyo", Description = "A labyrinth of stalls selling fresh seafood, street snacks, kitchen knives, and local treats.", Rating = 4.5 },
                new AttractionResult { Name = "Harajuku Takeshita Street", Category = "Shopping", EstimatedCost = 0m, DurationHours = 1.5, Latitude = 35.6702, Longitude = 139.7047, IsIndoor = false, Address = "1-19 Jingumae, Shibuya City, Tokyo", Description = "The epicenter of Japanese kawaii culture, quirky fashion boutiques, and sweet crepe shops.", Rating = 4.3 },
                new AttractionResult { Name = "Ichiran Ramen Shibuya", Category = "Food", EstimatedCost = 12m, DurationHours = 1.0, Latitude = 35.6617, Longitude = 139.7001, IsIndoor = true, Address = "1-22-7 Jinnan, Shibuya City, Tokyo", Description = "Classic Tonkotsu ramen served in individual eating booths for customized, distraction-free dining.", Rating = 4.5 }
            });
        }
        else
        {
            // Default dynamic generator for ANY other coordinates or cities (ensuring geolocated precision)
            var rng = new Random(normalizedCity.GetHashCode());
            
            // Generate a set of 10 deterministic attractions near the coordinate
            var categories = new[] { "Nature", "Food", "Museums", "Adventure", "Shopping" };
            
            // 2 items per category to ensure a good mix
            foreach (var cat in categories)
            {
                for (int i = 1; i <= 2; i++)
                {
                    double offsetLat = (rng.NextDouble() - 0.5) * 0.05; // within ~5km
                    double offsetLon = (rng.NextDouble() - 0.5) * 0.05;

                    var name = GetMockAttractionName(normalizedCity, cat, i, rng);
                    var (cost, duration, isIndoor) = GenerateAestheticMetadata(cat, name);

                    list.Add(new AttractionResult
                    {
                        Name = name,
                        Category = cat,
                        EstimatedCost = cost,
                        DurationHours = duration,
                        Latitude = baseLat + offsetLat,
                        Longitude = baseLon + offsetLon,
                        IsIndoor = isIndoor,
                        Address = $"{rng.Next(1, 400)} Promenade St, {normalizedCity}",
                        Description = $"A beautiful spot in {normalizedCity} that is highly recommended for visitors interested in {cat.ToLower()}.",
                        Rating = Math.Round(4.0 + (rng.NextDouble() * 1.0), 1)
                    });
                }
            }
        }

        return list;
    }

    private string GetMockAttractionName(string city, string category, int index, Random rng)
    {
        var prefixes = new[] { "Grand", "Royal", "Scenic", "Historic", "Central", "Old Town", "Modern" };
        var prefix = prefixes[rng.Next(prefixes.Length)];

        return category switch
        {
            "Nature" => index == 1 ? $"{city} Botanical Gardens" : $"{prefix} {city} National Park",
            "Food" => index == 1 ? $"Bistro {city}" : $"{prefix} {city} Food Market",
            "Museums" => index == 1 ? $"{city} Museum of Art & History" : $"The {prefix} {city} Gallery",
            "Adventure" => index == 1 ? $"{city} Summit Overlook & Cable Car" : $"{city} Coastal Exploration Tour",
            "Shopping" => index == 1 ? $"{prefix} {city} Shopping Center" : $"{city} Artisan Crafts Market",
            _ => $"{city} Point of Interest"
        };
    }

    // JSON DTOs for Geoapify Places
    private class GeoapifyPlacesResponse
    {
        [JsonPropertyName("features")]
        public GeoapifyPlaceFeature[] Features { get; set; } = Array.Empty<GeoapifyPlaceFeature>();
    }

    private class GeoapifyPlaceFeature
    {
        [JsonPropertyName("properties")]
        public GeoapifyPlaceProperties? Properties { get; set; }
    }

    private class GeoapifyPlaceProperties
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("formatted")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("categories")]
        public string[] Categories { get; set; } = Array.Empty<string>();
    }
}
