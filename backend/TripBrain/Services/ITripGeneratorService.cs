using System.Collections.Generic;
using System.Threading.Tasks;
using TripBrain.Models;

namespace TripBrain.Services;

public interface ITripGeneratorService
{
    Task<Trip> GenerateTripPlanAsync(string destination, string budget, int days, List<string> interests);
}
