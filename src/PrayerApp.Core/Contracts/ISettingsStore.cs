using PrayerApp.Core.Domain;

namespace PrayerApp.Core.Contracts;

/// <summary>Persistent storage for <see cref="UserSettings"/>. Implementations must never throw on read;
/// corrupt data is replaced with sane defaults.</summary>
public interface ISettingsStore
{
    UserSettings Get();
    void Save(UserSettings settings);
}
