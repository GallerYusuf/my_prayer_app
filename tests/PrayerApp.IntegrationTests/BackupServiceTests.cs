using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;
using PrayerApp.Infrastructure.Database;
using PrayerApp.Infrastructure.Repositories;
using PrayerApp.Infrastructure.Services;
using Xunit;

namespace PrayerApp.IntegrationTests;

public sealed class BackupServiceTests : IAsyncLifetime
{
    private readonly string _srcDbPath = Path.Combine(Path.GetTempPath(), $"prayerapp_src_{Guid.NewGuid():N}.db3");
    private readonly string _destDbPath = Path.Combine(Path.GetTempPath(), $"prayerapp_dest_{Guid.NewGuid():N}.db3");
    
    private AppDatabase _srcDatabase = null!;
    private AppDatabase _destDatabase = null!;

    private readonly FakeKeyValueStore _srcPrefs = new();
    private readonly FakeKeyValueStore _destPrefs = new();

    public Task InitializeAsync()
    {
        _srcDatabase = new AppDatabase(_srcDbPath);
        _destDatabase = new AppDatabase(_destDbPath);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _srcDatabase.DisposeAsync();
        await _destDatabase.DisposeAsync();

        if (File.Exists(_srcDbPath))
            File.Delete(_srcDbPath);

        if (File.Exists(_destDbPath))
            File.Delete(_destDbPath);
    }

    [Fact]
    public async Task ExportAndImport_RoundTrips_DatabaseAndPreferences()
    {
        // 1. Populate source database (e.g. Khatm plan, favorite verses)
        var connection = await _srcDatabase.GetConnectionAsync();
        await connection.ExecuteAsync(
            "INSERT INTO favorite_verses (SurahNumber, VerseNumber, AddedUnixSeconds) VALUES (?, ?, ?)",
            2, 255, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        // 2. Populate source preferences
        _srcPrefs.SetString("user_settings_v2", "{\"Theme\":\"Koyu\"}");
        _srcPrefs.SetString("prayer_track_streak", "15");

        // 3. Export backup from source
        var srcBackupService = new BackupService(_srcDatabase, _srcPrefs);
        var jsonBackup = await srcBackupService.ExportBackupAsync();

        Assert.False(string.IsNullOrWhiteSpace(jsonBackup));

        // Let's ensure the database file is physically created for the destination first
        // so that it can be overwritten safely.
        await _destDatabase.GetConnectionAsync();

        // 4. Import backup into destination
        var destBackupService = new BackupService(_destDatabase, _destPrefs);
        var success = await destBackupService.ImportBackupAsync(jsonBackup);

        Assert.True(success);

        // 5. Verify destination database data
        var destConnection = await _destDatabase.GetConnectionAsync();
        var count = await destConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM favorite_verses");
        var surah = await destConnection.ExecuteScalarAsync<int>("SELECT SurahNumber FROM favorite_verses LIMIT 1");
        var verse = await destConnection.ExecuteScalarAsync<int>("SELECT VerseNumber FROM favorite_verses LIMIT 1");

        Assert.Equal(1, count);
        Assert.Equal(2, surah);
        Assert.Equal(255, verse);

        // 6. Verify destination preferences
        Assert.Equal("{\"Theme\":\"Koyu\"}", _destPrefs.GetString("user_settings_v2"));
        Assert.Equal("15", _destPrefs.GetString("prayer_track_streak"));
    }

    [Fact]
    public async Task Export_IncludesFullTrackingHistory_KazaAndSurahPositions()
    {
        var oldDay = DateOnly.FromDateTime(DateTime.Today).AddDays(-40);
        _srcPrefs.SetString(StorageKeys.PrayerTrackDay(oldDay), "[\"Fajr\",\"Isha\"]");
        _srcPrefs.SetString(StorageKeys.PrayerTrackFirstDate, oldDay.ToString("yyyy-MM-dd"));
        _srcPrefs.SetString(StorageKeys.KazaCounters, "{\"Fajr\":12}");
        _srcPrefs.SetString(StorageKeys.QuranSurahPosition(36), "40");
        _srcPrefs.SetString("notification_scheduled_ids", "1,2,3"); // device-specific, must not travel

        await _srcDatabase.GetConnectionAsync();
        var json = await new BackupService(_srcDatabase, _srcPrefs).ExportBackupAsync();
        await _destDatabase.GetConnectionAsync();
        var success = await new BackupService(_destDatabase, _destPrefs).ImportBackupAsync(json);

        Assert.True(success);
        Assert.Equal("[\"Fajr\",\"Isha\"]", _destPrefs.GetString(StorageKeys.PrayerTrackDay(oldDay)));
        Assert.Equal("{\"Fajr\":12}", _destPrefs.GetString(StorageKeys.KazaCounters));
        Assert.Equal("40", _destPrefs.GetString(StorageKeys.QuranSurahPosition(36)));
        Assert.Null(_destPrefs.GetString("notification_scheduled_ids"));
    }

    [Fact]
    public async Task Import_CorruptDatabase_IsRejected_AndExistingDataIsKept()
    {
        var connection = await _destDatabase.GetConnectionAsync();
        await connection.ExecuteAsync(
            "INSERT INTO favorite_verses (SurahNumber, VerseNumber, AddedUnixSeconds) VALUES (?, ?, ?)", 1, 1, 0L);
        _destPrefs.SetString(StorageKeys.KazaCounters, "{\"Fajr\":3}");

        var garbage = JsonSerializer.Serialize(new
        {
            Version = 2,
            DatabaseBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }),
            Preferences = new Dictionary<string, string> { [StorageKeys.KazaCounters] = "{\"Fajr\":999}" }
        });

        var success = await new BackupService(_destDatabase, _destPrefs).ImportBackupAsync(garbage);

        Assert.False(success);
        var dest = await _destDatabase.GetConnectionAsync();
        Assert.Equal(1, await dest.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM favorite_verses"));
        Assert.Equal("{\"Fajr\":3}", _destPrefs.GetString(StorageKeys.KazaCounters));
    }

    [Fact]
    public async Task Import_NewerBackupVersion_IsRejected()
    {
        await _srcDatabase.GetConnectionAsync();
        var json = await new BackupService(_srcDatabase, _srcPrefs).ExportBackupAsync();
        var newer = json.Replace("\"Version\": 2", "\"Version\": 99");
        await _destDatabase.GetConnectionAsync();

        Assert.False(await new BackupService(_destDatabase, _destPrefs).ImportBackupAsync(newer));
    }

    private sealed class FakeKeyValueStore : IKeyValueStore
    {
        public Dictionary<string, string> Storage { get; } = new();
        public string? GetString(string key) => Storage.TryGetValue(key, out var val) ? val : null;
        public void SetString(string key, string value) => Storage[key] = value;
        public void Remove(string key) => Storage.Remove(key);
    }
}
