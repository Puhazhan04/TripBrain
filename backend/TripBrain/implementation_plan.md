# Implementation Plan: Date Input, CHF Pricing, Adventure Sight Filtering

## Goal Description

Enhance the **TripBrain** backend so that:
1. **Date Input** – The API accepts a start date for the trip, enabling weather forecasts that align with the requested dates.
2. **Swiss Franc Pricing** – All monetary values are presented in CHF instead of the default currency.
3. **Adventure & Sights Filtering** – When the user selects the "Adventure & Sights" interest, the generated itinerary prioritizes natural attractions (waterfalls, hiking trails, nature parks) and excludes museums.

## User Review Required

> [!IMPORTANT]
> - **Currency Conversion**: Do you prefer a **fixed conversion rate** (e.g., 1 USD = 0.92 CHF) or integration with a **live exchange‑rate API** (e.g., exchangerate.host)?
> - **Start Date Requirement**: Should `StartDate` be mandatory in `TripRequest` or optional with a default of today?

## Open Questions

> [!WARNING]
> 1. Should the frontend also be updated to include a date picker, or is backend‑only sufficient for the current prototype?
> 2. Do we need to store the original currency of each attraction, or can we assume all incoming costs are in USD and simply convert to CHF?

## Proposed Changes

---
### 1. DTOs
#### [MODIFY] [TripRequest.cs](file:///c:/Users/Puhazhan%20Indrakumar/TripBrain/backend/TripBrain.Api/DTOs/TripRequest.cs)
- Add a `DateTime StartDate` property with `[Required]` attribute (or make it optional based on user decision).
- Update documentation/comments.

---
### 2. Services
#### [MODIFY] [TripGeneratorService.cs](file:///c:/Users/Puhazhan%20Indrakumar/TripBrain/backend/TripBrain.Api/Services/TripGeneratorService.cs)
- Extend `GenerateTripPlanAsync` signature to accept `DateTime startDate` (or read it from a new request model).
- Pass `startDate` to the weather service when fetching forecasts.
- After calculating `TotalEstimatedCost`, convert each `EstimatedCost` to CHF via a new `ICurrencyConversionService`.
- Adjust `FilterByBudget` to compare against CHF thresholds (e.g., economy ≤ 25 CHF, mid‑range ≤ 55 CHF).
- Add a helper `FilterAdventureAttractions(List<AttractionResult> pool, List<string> interests)` that:
  * Removes attractions where `Category == "Museum"` when interests contain "Adventure" or "Sights".
  * Prioritises categories like `"Waterfall"`, `"Hiking Trail"`, `"Nature"`, `"Park"`.
- Update `GenerateDayActivities` to call the new filter before selecting spots.

---
### 3. New Currency Conversion Service
#### [NEW] [ICurrencyConversionService.cs](file:///c:/Users/Puhazhan%20Indrakumar/TripBrain/backend/TripBrain.Api/Services/ICurrencyConversionService.cs)
```csharp
public interface ICurrencyConversionService
{
    decimal ConvertToCHF(decimal amount, string fromCurrency = "USD");
}
```
#### [NEW] [CurrencyConversionService.cs](file:///c:/Users/Puhazhan%20Indrakumar/TripBrain/backend/TripBrain.Api/Services/CurrencyConversionService.cs)
```csharp
public class CurrencyConversionService : ICurrencyConversionService
{
    private const decimal FixedRate = 0.92m; // USD → CHF (adjustable)
    public decimal ConvertToCHF(decimal amount, string fromCurrency = "USD")
    {
        // For simplicity we use a fixed rate. Replace with live API call if desired.
        return Math.Round(amount * FixedRate, 2);
    }
}
```

---
### 4. Weather Service Update (if needed)
#### [MODIFY] [IWeatherService.cs] / [WeatherService.cs]
- Add overload `Task<List<WeatherData>> GetWeatherForecastAsync(double lat, double lon, DateTime startDate, int days);`
- Internally request forecast starting at `startDate` from the underlying provider (e.g., OpenWeatherMap). If the provider only supplies a 7‑day forecast, we can offset the list based on `startDate`.

---
### 5. Attraction Service Adjustment
#### [MODIFY] [IAttractionService.cs] / [AttractionService.cs]
- In the method that retrieves attractions, after the raw list is fetched, apply `FilterAdventureAttractions` when `interests` contain keywords "Adventure" or "Sights".
- Ensure the returned list respects the new budget filtering.

---
### 6. Dependency Injection Registration
#### [MODIFY] [Program.cs] (or Startup)
```csharp
services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
```
- Ensure the weather service overload is registered if a new implementation is added.

---
### 7. Tests
#### [NEW] [TripGeneratorServiceTests.cs]
- Verify that providing a `StartDate` results in `DayPlan.Date` aligning with the requested range.
- Assert that `TotalEstimatedCost` is expressed in CHF (using the conversion service).
- Test that with `Interests = ["Adventure & Sights"]` the generated attractions contain at least one `Category` from the natural list and contain **no** `Category == "Museum"`.

## Verification Plan

### Automated Tests
- Run existing unit tests plus the new ones.
- Check that `Trip.TotalEstimatedCost` is a CHF value (e.g., >0 and matches conversion logic).
- Ensure `DayPlan.Date` equals `StartDate` + offset days.
- Confirm adventure filtering works as intended.

### Manual Verification
- Issue a POST to `/api/trip` with JSON:
```json
{
  "DestinationCity": "Zürich",
  "Budget": "Mid-range",
  "DurationDays": 3,
  "StartDate": "2024-07-01",
  "Interests": ["Adventure & Sights"]
}
```
- Inspect response:
  * Dates should be `2024-07-01`, `2024-07-02`, `2024-07-03`.
  * All `EstimatedCost` fields should be in CHF.
  * Attractions should include waterfalls, hiking trails, etc., and exclude museums.

---
**End of Implementation Plan**
