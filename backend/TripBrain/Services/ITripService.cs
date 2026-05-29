using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TripBrain.Models;

namespace TripBrain.Services;

public interface ITripService
{
    Task<Trip> SaveTripAsync(Trip trip);
    Task<List<Trip>> GetAllTripsAsync();
    Task<Trip?> GetTripByIdAsync(Guid id);
    Task<bool> DeleteTripAsync(Guid id);
}
