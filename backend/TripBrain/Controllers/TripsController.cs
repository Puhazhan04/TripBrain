using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TripBrain.DTOs;
using TripBrain.Models;
using TripBrain.Services;

namespace TripBrain.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly ITripGeneratorService _tripGenerator;
    private readonly ITripService _tripService;

    public TripsController(ITripGeneratorService tripGenerator, ITripService tripService)
    {
        _tripGenerator = tripGenerator;
        _tripService = tripService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TripResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTrip([FromBody] TripRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // 1. Generate the smart trip plan (weather-aware and geographic clustered)
            var trip = await _tripGenerator.GenerateTripPlanAsync(
                request.DestinationCity,
                request.Budget,
                request.DurationDays,
                request.Interests
            );

            // 2. Save it to SQLite
            var savedTrip = await _tripService.SaveTripAsync(trip);

            // 3. Return mapped DTO response
            var response = MapToResponse(savedTrip);
            return CreatedAtAction(nameof(GetTripById), new { id = savedTrip.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while generating the trip.", details = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TripResponse>))]
    public async Task<IActionResult> GetAllTrips()
    {
        var trips = await _tripService.GetAllTripsAsync();
        var response = trips.Select(MapToResponseSummary);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TripResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTripById(Guid id)
    {
        var trip = await _tripService.GetTripByIdAsync(id);
        if (trip == null)
        {
            return NotFound(new { message = $"Trip with ID '{id}' was not found." });
        }

        return Ok(MapToResponse(trip));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var deleted = await _tripService.DeleteTripAsync(id);
        if (!deleted)
        {
            return NotFound(new { message = $"Trip with ID '{id}' was not found." });
        }

        return NoContent();
    }

    private static TripResponse MapToResponse(Trip trip)
    {
        return new TripResponse
        {
            Id = trip.Id,
            DestinationCity = trip.DestinationCity,
            Latitude = trip.Latitude,
            Longitude = trip.Longitude,
            Budget = trip.Budget,
            DurationDays = trip.DurationDays,
            Interests = trip.Interests,
            CreatedAt = trip.CreatedAt,
            TotalEstimatedCost = trip.TotalEstimatedCost,
            DayPlans = trip.DayPlans.Select(dp => new DayPlanResponse
            {
                Id = dp.Id,
                DayNumber = dp.DayNumber,
                Date = dp.Date,
                WeatherSummary = dp.WeatherSummary,
                WeatherTemp = dp.WeatherTemp,
                WeatherConditionCode = dp.WeatherConditionCode,
                WeatherIcon = dp.WeatherIcon,
                Activities = dp.Activities.Select(a => new DayActivityResponse
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Category = a.Category,
                    EstimatedCost = a.EstimatedCost,
                    DurationHours = a.DurationHours,
                    RecommendedTime = a.RecommendedTime,
                    Latitude = a.Latitude,
                    Longitude = a.Longitude,
                    IsIndoor = a.IsIndoor,
                    Address = a.Address,
                    Rating = a.Rating
                }).ToList()
            }).ToList()
        };
    }

    private static TripResponse MapToResponseSummary(Trip trip)
    {
        return new TripResponse
        {
            Id = trip.Id,
            DestinationCity = trip.DestinationCity,
            Latitude = trip.Latitude,
            Longitude = trip.Longitude,
            Budget = trip.Budget,
            DurationDays = trip.DurationDays,
            Interests = trip.Interests,
            CreatedAt = trip.CreatedAt,
            TotalEstimatedCost = trip.TotalEstimatedCost
            // Exclude DayPlans for lists to save bandwidth
        };
    }
}
