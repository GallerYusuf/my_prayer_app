namespace PrayerApp.Core.Contracts;

/// <summary>A Quran completion (khatm) plan tracked by mushaf page (1-604).</summary>
public sealed record KhatmPlan(
    int Id,
    DateOnly StartDate,
    int TargetDays,
    int LastReadPage,
    DateOnly? CompletedDate)
{
    public const int TotalPages = 604;

    public bool IsCompleted => CompletedDate is not null;

    public double Progress => Math.Clamp(LastReadPage / (double)TotalPages, 0, 1);

    public int RemainingPages => Math.Max(0, TotalPages - LastReadPage);

    /// <summary>Pages/day needed to finish on time; 0 when no deadline or already done.</summary>
    public int PagesPerDayTarget(DateOnly today)
    {
        if (TargetDays <= 0 || IsCompleted || RemainingPages == 0)
            return 0;

        var remainingDays = StartDate.AddDays(TargetDays).DayNumber - today.DayNumber;
        return remainingDays <= 0
            ? RemainingPages
            : (int)Math.Ceiling(RemainingPages / (double)remainingDays);
    }
}

/// <summary>Local (SQLite) storage for khatm plans and their daily reading log.</summary>
public interface IKhatmRepository
{
    /// <summary>The single in-progress plan, or null if none.</summary>
    Task<KhatmPlan?> GetActivePlanAsync(CancellationToken cancellationToken = default);

    /// <summary>Starts a new plan; any previous unfinished plan is abandoned.</summary>
    Task<KhatmPlan> StartPlanAsync(int targetDays, DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the last read page (clamped to 0-604), records today's log entry
    /// and marks the plan completed when page 604 is reached.
    /// </summary>
    Task<KhatmPlan> SetLastReadPageAsync(int planId, int page, DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>Pages read on the given day according to the daily log.</summary>
    Task<int> GetPagesReadOnAsync(int planId, DateOnly day, CancellationToken cancellationToken = default);

    /// <summary>Completed plans, most recent first.</summary>
    Task<IReadOnlyList<KhatmPlan>> GetCompletedPlansAsync(int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>Deletes the plan and its log.</summary>
    Task DeletePlanAsync(int planId, CancellationToken cancellationToken = default);
}
