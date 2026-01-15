using System.Globalization;
using System.Text.Json;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class GeocodingService(HttpClient httpClient, ISettingsService settingsService, ILogger<GeocodingService> logger) : IGeocodingService
{
    public virtual async Task<string?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.MapboxToken)) return null;

        try
        {
            var latStr = latitude.ToString(CultureInfo.InvariantCulture);
            var lngStr = longitude.ToString(CultureInfo.InvariantCulture);
            var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{lngStr},{latStr}.json?access_token={settings.MapboxToken}&types=address,poi,neighborhood&limit=1";
            
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var features = doc.RootElement.GetProperty("features");
            if (features.GetArrayLength() > 0)
            {
                return features[0].GetProperty("place_name").GetString();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ReverseGeocodeAsync for {Lat}, {Lng}", latitude, longitude);
        }

        return null;
    }

    public virtual async Task<List<GeocodingResult>> SearchAsync(string query)
    {
        var settings = await settingsService.GetSettingsAsync();
        if (string.IsNullOrEmpty(settings.MapboxToken) || string.IsNullOrWhiteSpace(query)) return new List<GeocodingResult>();

        try
        {
            var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{Uri.EscapeDataString(query)}.json?access_token={settings.MapboxToken}&limit=5";
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return new List<GeocodingResult>();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var features = doc.RootElement.GetProperty("features");
            var results = new List<GeocodingResult>();
            foreach (var feature in features.EnumerateArray())
            {
                var center = feature.GetProperty("center");
                results.Add(new GeocodingResult
                {
                    Address = feature.GetProperty("place_name").GetString() ?? "",
                    Longitude = center[0].GetDouble(),
                    Latitude = center[1].GetDouble()
                });
            }
            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in Geocoding SearchAsync for '{Query}'", query);
        }

        return new List<GeocodingResult>();
    }
}
