using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PrayerApp.Core.Contracts;
using PrayerApp.Infrastructure.Database;

namespace PrayerApp.Infrastructure.Services;

public sealed class BackupService(AppDatabase database, IKeyValueStore keyValueStore) : IBackupService
{
    private sealed class BackupData
    {
        public int Version { get; set; } = 1;
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string DatabaseBase64 { get; set; } = string.Empty;
        public Dictionary<string, string> Preferences { get; set; } = new();
    }

    public async Task<string> ExportBackupAsync()
    {
        // 1. Close connection to flush SQLite journals/WAL to disk
        await database.CloseConnectionAsync();

        // 2. Read database file
        var dbPath = database.DatabasePath;
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("Database file not found", dbPath);

        byte[] dbBytes = await File.ReadAllBytesAsync(dbPath);
        string dbBase64 = Convert.ToBase64String(dbBytes);

        // 3. Read specific user preferences
        var prefs = new Dictionary<string, string>();
        var keys = new[]
        {
            "user_settings_v2",
            "prayer_track_streak",
            "prayer_track_last_full",
            $"prayer_track_{DateTime.Today:yyyy-MM-dd}"
        };

        foreach (var key in keys)
        {
            var val = keyValueStore.GetString(key);
            if (!string.IsNullOrEmpty(val))
                prefs[key] = val;
        }

        var backup = new BackupData
        {
            DatabaseBase64 = dbBase64,
            Preferences = prefs
        };

        return JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<bool> ImportBackupAsync(string jsonContent)
    {
        try
        {
            var backup = JsonSerializer.Deserialize<BackupData>(jsonContent);
            if (backup == null || string.IsNullOrWhiteSpace(backup.DatabaseBase64))
                return false;

            // 1. Close active database connection
            await database.CloseConnectionAsync();

            // 2. Write SQLite database file bytes
            var dbPath = database.DatabasePath;
            byte[] dbBytes = Convert.FromBase64String(backup.DatabaseBase64);
            await File.WriteAllBytesAsync(dbPath, dbBytes);

            // 3. Restore preferences
            foreach (var pair in backup.Preferences)
            {
                keyValueStore.SetString(pair.Key, pair.Value);
            }

            // 4. Force re-init connection and check migrations
            await database.GetConnectionAsync();

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
