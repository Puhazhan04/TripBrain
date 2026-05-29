using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TripBrain.DTOs;

public class TripRequest
{
    [Required(ErrorMessage = "Destination city is required.")]
    [StringLength(100, ErrorMessage = "Destination name cannot exceed 100 characters.")]
    public string DestinationCity { get; set; } = string.Empty;

    [Required(ErrorMessage = "Budget is required.")]
    [RegularExpression("^(Economy|Mid-range|Luxury)$", ErrorMessage = "Budget must be Economy, Mid-range, or Luxury.")]
    public string Budget { get; set; } = "Mid-range";

    [Range(1, 14, ErrorMessage = "Duration must be between 1 and 14 days.")]
    public int DurationDays { get; set; } = 3;

    /// <summary>
    /// Optional start date for the trip. Defaults to today if not provided.
    /// </summary>
    public DateTime StartDate { get; set; } = DateTime.Today;

    public List<string> Interests { get; set; } = new();
}
