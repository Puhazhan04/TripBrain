using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TripBrain.Data;
using TripBrain.Models;

namespace TripBrain.Services;

public class TripService : ITripService
{
    private readonly AppDbContext _context;

    public TripService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Trip> SaveTripAsync(Trip trip)
    {
        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();
        return trip;
    }

    public async Task<List<Trip>> GetAllTripsAsync()
    {
        return await _context.Trips
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Trip?> GetTripByIdAsync(Guid id)
    {
        return await _context.Trips
            .Include(t => t.DayPlans)
            .ThenInclude(dp => dp.Activities)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<bool> DeleteTripAsync(Guid id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip == null) return false;

        _context.Trips.Remove(trip);
        await _context.SaveChangesAsync();
        return true;
    }
}
