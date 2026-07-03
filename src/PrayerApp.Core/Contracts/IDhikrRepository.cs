namespace PrayerApp.Core.Contracts;

/// <summary>A dhikr counting session persisted locally.</summary>
public sealed record DhikrSession(
    int Id,
    string DhikrName,
    int Count,
    int TargetCount,
    DateOnly Date);

public interface IDhikrRepository
{
    Task<DhikrSession?> GetTodaySessionAsync(string dhikrName, DateOnly today, CancellationToken cancellationToken = default);
    Task<DhikrSession> UpsertAsync(DhikrSession session, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DhikrSession>> GetHistoryAsync(int limit = 30, CancellationToken cancellationToken = default);
}
