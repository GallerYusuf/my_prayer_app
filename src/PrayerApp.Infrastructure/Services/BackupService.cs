using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Infrastructure.Database;
using SQLite;

namespace PrayerApp.Infrastructure.Services;

/// <summary>
/// Local JSON backup: the SQLite database file plus the user-data preference keys
/// listed in <see cref="StorageKeys"/>.
/// <para>
/// Restore is defensive: the payload is fully validated (SQLite header, integrity check,
/// schema version, expected tables) <i>before</i> the live database is touched, the current
/// database is kept as a fallback while swapping, and only whitelisted preference keys are
/// written back. Version 1 backups (created before this format) are still accepted.
/// </para>
/// </summary>
public sealed class BackupService(AppDatabase database, IKeyValueStore keyValueStore) : IBackupService
{
    public const int CurrentVersion = 2;

    /// <summary>
    /// Tracking history scan window for installs that predate <see cref="StorageKeys.PrayerTrackFirstDate"/>.
    /// </summary>
    private const int LegacyTrackingScanDays = 3 * 366;

    private static readonly byte[] SqliteHeader = "SQLite format 3\0"u8.ToArray();
    private static readonly string[] SqliteSidecarSuffixes = ["-journal", "-wal", "-shm"];

    private sealed class BackupData
    {
        public int Version { get; set; } = CurrentVersion;
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        public string DatabaseBase64 { get; set; } = string.Empty;
        public Dictionary<string, string> Preferences { get; set; } = new();
    }

    public async Task<string> ExportBackupAsync()
    {
        // Close the connection so journals are flushed and the file is consistent on disk.
        await database.CloseConnectionAsync();

        var dbPath = database.DatabasePath;
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("Database file not found", dbPath);

        var dbBytes = await File.ReadAllBytesAsync(dbPath);

        var backup = new BackupData
        {
            DatabaseBase64 = Convert.ToBase64String(dbBytes),
            Preferences = CollectPreferences()
        };

        return JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<bool> ImportBackupAsync(string jsonContent)
    {
        BackupData? backup;
        byte[] dbBytes;
        try
        {
            backup = JsonSerializer.Deserialize<BackupData>(jsonContent);
            if (backup is null ||
                backup.Version is < 1 or > CurrentVersion ||
                string.IsNullOrWhiteSpace(backup.DatabaseBase64))
            {
                return false;
            }

            dbBytes = Convert.FromBase64String(backup.DatabaseBase64);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or NotSupportedException)
        {
            return false;
        }

        var dbPath = database.DatabasePath;
        var stagedPath = dbPath + ".restore-new";
        var fallbackPath = dbPath + ".restore-old";

        try
        {
            // 1. Validate the payload in a separate file; the live database is untouched so far.
            await File.WriteAllBytesAsync(stagedPath, dbBytes);
            if (!IsValidDatabaseFile(stagedPath, dbBytes))
                return false;

            // 2. Swap in the restored database, keeping the current one as a fallback.
            await database.CloseConnectionAsync();
            var hadExisting = File.Exists(dbPath);
            if (hadExisting)
                File.Copy(dbPath, fallbackPath, overwrite: true);

            try
            {
                DeleteSidecars(dbPath);
                File.Move(stagedPath, dbPath, overwrite: true);

                // Re-open; runs migrations so older backups are upgraded in place.
                await database.GetConnectionAsync();
            }
            catch
            {
                await database.CloseConnectionAsync();
                DeleteSidecars(dbPath);
                if (hadExisting)
                    File.Copy(fallbackPath, dbPath, overwrite: true);
                else
                    TryDelete(dbPath);

                await database.GetConnectionAsync();
                return false;
            }

            // 3. Database is in place; restore only whitelisted user-data preferences.
            foreach (var (key, value) in backup.Preferences)
            {
                if (StorageKeys.IsRestorable(key) && value is not null)
                    keyValueStore.SetString(key, value);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            TryDelete(stagedPath);
            TryDelete(fallbackPath);
        }
    }

    private Dictionary<string, string> CollectPreferences()
    {
        var prefs = new Dictionary<string, string>();

        foreach (var key in StorageKeys.BackedUpKeys)
            AddIfPresent(prefs, key);

        for (var surah = 1; surah <= StorageKeys.SurahCount; surah++)
            AddIfPresent(prefs, StorageKeys.QuranSurahPosition(surah));

        // Preferences cannot be enumerated, so walk the tracked date range explicitly.
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = today.AddDays(-LegacyTrackingScanDays);
        var firstRaw = keyValueStore.GetString(StorageKeys.PrayerTrackFirstDate);
        if (DateOnly.TryParseExact(firstRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first)
            && first < start)
        {
            start = first;
        }

        for (var date = start; date <= today; date = date.AddDays(1))
            AddIfPresent(prefs, StorageKeys.PrayerTrackDay(date));

        return prefs;
    }

    private void AddIfPresent(Dictionary<string, string> prefs, string key)
    {
        var value = keyValueStore.GetString(key);
        if (!string.IsNullOrEmpty(value))
            prefs[key] = value;
    }

    private static bool IsValidDatabaseFile(string path, byte[] bytes)
    {
        if (bytes.Length < SqliteHeader.Length || !bytes.AsSpan(0, SqliteHeader.Length).SequenceEqual(SqliteHeader))
            return false;

        try
        {
            using var connection = new SQLiteConnection(path, SQLiteOpenFlags.ReadOnly);

            if (!string.Equals(connection.ExecuteScalar<string>("PRAGMA quick_check"), "ok", StringComparison.OrdinalIgnoreCase))
                return false;

            // A backup from a newer app version could have a schema this build cannot read.
            var version = connection.ExecuteScalar<int>("PRAGMA user_version");
            if (version is < 1 or > AppDatabase.CurrentSchemaVersion)
                return false;

            var hasCoreTable = connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'prayer_days'");
            return hasCoreTable == 1;
        }
        catch (SQLiteException)
        {
            return false;
        }
    }

    private static void DeleteSidecars(string dbPath)
    {
        foreach (var suffix in SqliteSidecarSuffixes)
            TryDelete(dbPath + suffix);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Best effort cleanup of temporary files.
        }
    }
}
