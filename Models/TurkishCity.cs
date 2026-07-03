namespace PrayerApp.Models;

public sealed class TurkishCity
{
    public required int PlateCode { get; init; }
    public required string DisplayName { get; init; }
    public required string ApiName { get; init; }
    public required string Region { get; init; }
    public IReadOnlyList<TurkishDistrict> Districts { get; init; } = [];

    public string PlateLabel => PlateCode.ToString("D2");
    public int DistrictCount => Districts.Count;
}

public sealed class TurkishDistrict
{
    public required int PlateCode { get; init; }
    public required string DisplayName { get; init; }
    public required string ApiName { get; init; }
}

public sealed record CitySelection(TurkishCity City, TurkishDistrict? District)
{
    public string Display => District is null
        ? City.DisplayName
        : $"{District.DisplayName}, {City.DisplayName}";

    public string Query => District is null
        ? City.ApiName
        : $"{District.ApiName}, {City.ApiName}";
}
