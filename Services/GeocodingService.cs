using System.Globalization;
using System.Text.Json;
using Polly;
using Polly.Fallback;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class GeocodingService(HttpClient httpClient, ISettingsService settingsService, ILogger<GeocodingService> logger) : IGeocodingService
{
    private const string NominatimUserAgent = "WhereAreThey/1.0 (https://www.aretheyhere.com)";
    
    private static readonly ResiliencePropertyKey<double> LatKey = new("lat");
    private static readonly ResiliencePropertyKey<double> LngKey = new("lng");
    private static readonly ResiliencePropertyKey<string> QueryKey = new("query");
    private static readonly ResiliencePropertyKey<GeocodingService> ServiceKey = new("service");

    private readonly ResiliencePipeline<string?> _reverseGeocodePipeline = new ResiliencePipelineBuilder<string?>()
        .AddFallback(new FallbackStrategyOptions<string?>
        {
            ShouldHandle = new PredicateBuilder<string?>()
                .Handle<Exception>()
                .HandleResult(null as string),
            FallbackAction = async args => 
            {
                args.Context.Properties.TryGetValue(LatKey, out var lat);
                args.Context.Properties.TryGetValue(LngKey, out var lng);
                args.Context.Properties.TryGetValue(ServiceKey, out var service);
                var result = await service!.GetNominatimReverseAsync(lat, lng);
                return Outcome.FromResult(result);
            },
            OnFallback = args => 
            {
                logger.LogWarning("Mapbox ReverseGeocodeAsync failed or returned no result. Falling back to Nominatim.");
                return default;
            },
        })
        .Build();

    private readonly ResiliencePipeline<List<GeocodingResult>> _searchPipeline = new ResiliencePipelineBuilder<List<GeocodingResult>>()
        .AddFallback(new FallbackStrategyOptions<List<GeocodingResult>>
        {
            ShouldHandle = new PredicateBuilder<List<GeocodingResult>>()
                .Handle<Exception>()
                .HandleResult(results => results == null || results.Count == 0),
            FallbackAction = async args =>
            {
                args.Context.Properties.TryGetValue(QueryKey, out var query);
                args.Context.Properties.TryGetValue(ServiceKey, out var service);
                var result = await service!.GetNominatimSearchAsync(query!);
                return Outcome.FromResult(result);
            },
            OnFallback = args =>
            {
                logger.LogWarning("Mapbox SearchAsync failed or returned no results. Falling back to Nominatim.");
                return default;
            },
        })
        .Build();

    /// <inheritdoc />
    public virtual async Task<string?> ReverseGeocodeAsync(double latitude, double longitude)
    {
        var settings = await settingsService.GetSettingsAsync();
        
        var context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(LatKey, latitude);
        context.Properties.Set(LngKey, longitude);
        context.Properties.Set(ServiceKey, this);

        try
        {
            return await _reverseGeocodePipeline.ExecuteAsync(async _ => 
            {
                if (string.IsNullOrEmpty(settings.MapboxToken))
                {
                    return null;
                }
                return await GetMapboxReverseAsync(latitude, longitude, settings.MapboxToken);
            }, context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private async Task<string?> GetMapboxReverseAsync(double latitude, double longitude, string token)
    {
        var latStr = latitude.ToString(CultureInfo.InvariantCulture);
        var lngStr = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{lngStr},{latStr}.json?access_token={token}&types=address,poi,neighborhood&limit=1";
        
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var features = doc.RootElement.GetProperty("features");
        if (features.GetArrayLength() > 0)
        {
            return features[0].GetProperty("place_name").GetString();
        }
        return null;
    }

    private async Task<string?> GetNominatimReverseAsync(double latitude, double longitude)
    {
        var latStr = latitude.ToString(CultureInfo.InvariantCulture);
        var lngStr = longitude.ToString(CultureInfo.InvariantCulture);
        var url = $"https://nominatim.openstreetmap.org/reverse?lat={latStr}&lon={lngStr}&format=jsonv2";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", NominatimUserAgent);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("display_name", out var displayName))
        {
            return displayName.GetString();
        }
        return null;
    }

    /// <inheritdoc />
    public virtual async Task<List<GeocodingResult>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var settings = await settingsService.GetSettingsAsync();
        
        var context = ResilienceContextPool.Shared.Get();
        context.Properties.Set(QueryKey, query);
        context.Properties.Set(ServiceKey, this);

        try
        {
            return await _searchPipeline.ExecuteAsync(async _ => 
            {
                if (string.IsNullOrEmpty(settings.MapboxToken))
                {
                    return [];
                }
                return await GetMapboxSearchAsync(query, settings.MapboxToken);
            }, context);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    private async Task<List<GeocodingResult>> GetMapboxSearchAsync(string query, string token)
    {
        var url = $"https://api.mapbox.com/geocoding/v5/mapbox.places/{Uri.EscapeDataString(query)}.json?access_token={token}&limit=5";
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

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
                Latitude = center[1].GetDouble(),
            });
        }
        return results;
    }

    private async Task<List<GeocodingResult>> GetNominatimSearchAsync(string query)
    {
        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=jsonv2&limit=5";
        
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", NominatimUserAgent);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = new List<GeocodingResult>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            results.Add(new GeocodingResult
            {
                Address = item.GetProperty("display_name").GetString() ?? "",
                Latitude = double.Parse(item.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture),
                Longitude = double.Parse(item.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture),
            });
        }
        return results;
    }
}
