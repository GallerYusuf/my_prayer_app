namespace PrayerApp.Core.Common;

/// <summary>Testable time source. Never use DateTime.Now directly in application code.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Current time in the device's local timezone.</summary>
    DateTimeOffset LocalNow { get; }

    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTimeOffset LocalNow => DateTimeOffset.Now;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}
