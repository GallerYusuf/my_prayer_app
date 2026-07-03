using Microsoft.Extensions.Logging;
using PrayerApp.Core.Common;
using PrayerApp.Core.Domain;

namespace PrayerApp.Services;

public interface ILocationService
{
    /// <summary>
    /// Resolves the device location: last-known first (fast), then a fresh fix.
    /// Returns a typed error when permission is denied or location can't be resolved,
    /// so the caller can offer manual city selection instead.
    /// </summary>
    Task<Result<GeoLocation>> GetCurrentLocationAsync(CancellationToken cancellationToken = default);
}

public sealed class LocationService(ILogger<LocationService> logger) : ILocationService
{
    public async Task<Result<GeoLocation>> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    return AppError.PermissionDenied(
                        "Konum izni verilmedi. Şehrinizi manuel olarak seçebilirsiniz.");
            }

            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            location ??= await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(15)),
                cancellationToken);

            if (location is null)
                return AppError.NotFound("Konum belirlenemedi. Şehrinizi manuel olarak seçebilirsiniz.");

            var displayName = await TryReverseGeocodeAsync(location.Latitude, location.Longitude);

            return new GeoLocation
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                DisplayName = displayName ?? "Mevcut Konum",
                IsFromDevice = true
            };
        }
        catch (FeatureNotEnabledException)
        {
            return AppError.NotFound("Cihaz konumu kapalı. Konumu açın veya şehrinizi manuel seçin.");
        }
        catch (FeatureNotSupportedException)
        {
            return AppError.NotFound("Bu cihaz konum özelliğini desteklemiyor. Şehrinizi manuel seçin.");
        }
        catch (PermissionException)
        {
            return AppError.PermissionDenied("Konum izni verilmedi. Şehrinizi manuel olarak seçebilirsiniz.");
        }
        catch (OperationCanceledException)
        {
            return AppError.Timeout("Konum alınırken zaman aşımı oluştu. Tekrar deneyin veya şehrinizi manuel seçin.");
        }
    }

    /// <summary>Reverse geocoding is best-effort; coordinates alone are enough for prayer times.</summary>
    private async Task<string?> TryReverseGeocodeAsync(double latitude, double longitude)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(latitude, longitude);
            var place = placemarks?.FirstOrDefault();
            if (place is null)
                return null;

            var district = place.SubAdminArea;
            var city = place.AdminArea ?? place.Locality;

            if (!string.IsNullOrWhiteSpace(district) && !string.IsNullOrWhiteSpace(city) && district != city)
                return $"{district}, {city}";

            return city ?? district;
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Reverse geocoding failed; continuing with coordinates");
            return null;
        }
    }
}
