using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
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

    private sealed class FakeKeyValueStore : IKeyValueStore
    {
        public Dictionary<string, string> Storage { get; } = new();
        public string? GetString(string key) => Storage.TryGetValue(key, out var val) ? val : null;
        public void SetString(string key, string value) => Storage[key] = value;
        public void Remove(string key) => Storage.Remove(key);
    }
}
