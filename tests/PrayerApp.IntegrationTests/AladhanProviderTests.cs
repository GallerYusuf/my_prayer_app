using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using PrayerApp.Core.Common;
using PrayerApp.Core.Domain;
using PrayerApp.Infrastructure.Providers;
using Xunit;

namespace PrayerApp.IntegrationTests;

public class AladhanProviderTests
{
    private static readonly GeoLocation Van = new()
    {
        Latitude = 38.5012,
        Longitude = 43.3730,
        DisplayName = "Van"
    };

    private static AladhanPrayerTimeProvider CreateProvider(FakeHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.aladhan.com/v1/") },
            NullLogger<AladhanPrayerTimeProvider>.Instance);

    [Fact]
    public async Task SuccessfulResponse_ReturnsRequestedRange()
    {
        var handler = new FakeHttpHandler(req =>
        {
            var url = req.RequestUri!.ToString();
            return AladhanJson.Ok(url.Contains("/2026/7") ? AladhanJson.Month(2026, 7) : AladhanJson.Month(2026, 8));
        });

        var provider = CreateProvider(handler);
        var result = await provider.GetPrayerDaysAsync(Van, CalculationMethod.Diyanet, new DateOnly(2026, 7, 20), 30);

        Assert.True(result.IsSuccess);
        Assert.Equal(30, result.Value.Count);
        Assert.Equal(new DateOnly(2026, 7, 20), result.Value[0].Date);
        Assert.Equal(new DateOnly(2026, 8, 18), result.Value[^1].Date);
        Assert.Equal(2, handler.RequestCount); // July + August
        Assert.All(handler.RequestedUrls, url => Assert.Contains("method=13", url));
        Assert.All(handler.RequestedUrls, url => Assert.Contains("latitude=38.501200", url));
    }

    [Fact]
    public async Task ServerError_RetriesOnceThenFails()
    {
        var handler = new FakeHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var provider = CreateProvider(handler);

        var result = await provider.GetPrayerDaysAsync(Van, CalculationMethod.Diyanet, new DateOnly(2026, 7, 1), 5);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Network, result.Error.Kind);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public async Task MalformedJson_ReturnsInvalidDataError()
    {
        var handler = new FakeHttpHandler(_ => AladhanJson.Ok("{ this is not json"));
        var provider = CreateProvider(handler);

        var result = await provider.GetPrayerDaysAsync(Van, CalculationMethod.Diyanet, new DateOnly(2026, 7, 1), 5);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.InvalidData, result.Error.Kind);
    }

    [Fact]
    public async Task MissingTimings_SkipsIncompleteDays()
    {
        const string json = """
        {"code":200,"status":"OK","data":[
          {"timings":{"Fajr":"04:30 (+03)","Sunrise":"06:00 (+03)","Dhuhr":"12:45 (+03)","Asr":"16:30 (+03)","Maghrib":"19:45 (+03)","Isha":"21:15 (+03)"},
           "date":{"gregorian":{"date":"01-07-2026"},"hijri":{"day":"1","year":"1447","month":{"number":1}}}},
          {"timings":{"Fajr":"bozuk","Sunrise":"06:00","Dhuhr":"12:45","Asr":"16:30","Maghrib":"19:45","Isha":"21:15"},
           "date":{"gregorian":{"date":"02-07-2026"},"hijri":{"day":"2","year":"1447","month":{"number":1}}}}
        ]}
        """;

        var handler = new FakeHttpHandler(_ => AladhanJson.Ok(json));
        var provider = CreateProvider(handler);

        var result = await provider.GetPrayerDaysAsync(Van, CalculationMethod.Diyanet, new DateOnly(2026, 7, 1), 2);

        Assert.True(result.IsSuccess);
        var day = Assert.Single(result.Value);
        Assert.Equal(new DateOnly(2026, 7, 1), day.Date);
        Assert.True(day.IsComplete);
    }

    [Fact]
    public async Task NetworkFailure_ReturnsNetworkError()
    {
        var handler = new FakeHttpHandler(_ => throw new HttpRequestException("no connection"));
        var provider = CreateProvider(handler);

        var result = await provider.GetPrayerDaysAsync(Van, CalculationMethod.Diyanet, new DateOnly(2026, 7, 1), 5);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Network, result.Error.Kind);
    }

    [Fact]
    public async Task NonSuccessApiCode_ReturnsInvalidData()
    {
        var handler = new FakeHttpHandler(_ => AladhanJson.Ok("""{"code":400,"status":"Bad Request","data":[]}"""));
        var provider = CreateProvider(handler);

        var result = await provider.GetPrayerDaysAsync(Van, CalculationMethod.Diyanet, new DateOnly(2026, 7, 1), 5);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.InvalidData, result.Error.Kind);
    }
}
