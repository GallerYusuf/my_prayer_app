using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Contracts;

/// <summary>Persistent storage for <see cref="UserSettings"/>. Implementations must never throw on read;
/// corrupt data is replaced with sane defaults.</summary>
public interface ISettingsStore
{
    UserSettings Get();
    void Save(UserSettings settings);

    /// <summary>
    /// Drops any in-memory copy so the next <see cref="Get"/> reads persisted data again
    /// (needed after a backup restore rewrites the underlying storage).
    /// </summary>
    void Reload() { }
}
