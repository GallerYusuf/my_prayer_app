using PrayerApp.Core.Domain;
using PrayerApp.Core.Notifications;
using Xunit;

namespace PrayerApp.UnitTests;

public class NotificationPlannerTests
{
    private static readonly DateOnly Today = new(2026, 7, 3); // Friday

    private static UserSettings DefaultSettings => new()
    {
        HasCompletedOnboarding = true,
        PrayerNotificationsEnabled = true,
        FridayNotificationsEnabled = false,
        VerseNotificationsEnabled = false,
        MinutesBeforePrayer = 0
    };

    private static PrayerDay[] ThreeDays =>
        [TestData.Day(Today), TestData.Day(Today.AddDays(1)), TestData.Day(Today.AddDays(2))];

    [Fact]
    public void BuildPlan_OnlyFutureNotifications()
    {
        var now = TestData.At(Today, "13:00");
        var plan = NotificationPlanner.BuildPlan(ThreeDays, DefaultSettings, now);

        Assert.NotEmpty(plan);
        Assert.All(plan, n => Assert.True(n.NotifyAt > now));
    }

    [Fact]
    public void BuildPlan_IsIdempotent()
    {
        var now = TestData.At(Today, "13:00");
        var plan1 = NotificationPlanner.BuildPlan(ThreeDays, DefaultSettings, now);
        var plan2 = NotificationPlanner.BuildPlan(ThreeDays, DefaultSettings, now);

        Assert.Equal(plan1.Select(n => (n.Id, n.NotifyAt)), plan2.Select(n => (n.Id, n.NotifyAt)));
    }

    [Fact]
    public void BuildPlan_IdsAreUnique()
    {
        var now = TestData.At(Today, "01:00");
        var settings = DefaultSettings with
        {
            MinutesBeforePrayer = 15,
            FridayNotificationsEnabled = true,
            VerseNotificationsEnabled = true,
            VerseNotificationTimes = [new TimeOnly(9, 0), new TimeOnly(20, 0)]
        };

        var plan = NotificationPlanner.BuildPlan(ThreeDays, settings, now,
            (date, slot) => ("Günün Ayeti", $"test {date} {slot}", ""));

        Assert.Equal(plan.Count, plan.Select(n => n.Id).Distinct().Count());
    }

    [Fact]
    public void BuildPlan_RespectsPerPrayerToggles()
    {
        var now = TestData.At(Today, "01:00");
        var settings = DefaultSettings with
        {
            PrayerNotificationToggles = new Dictionary<PrayerKind, bool>
            {
                [PrayerKind.Sunrise] = false,
                [PrayerKind.Dhuhr] = false
            }
        };

        var plan = NotificationPlanner.BuildPlan([TestData.Day(Today)], settings, now);
        var kinds = plan.Select(n => NotificationIds.GetCategory(n.Id)).ToList();

        Assert.Equal(4, plan.Count); // Fajr, Asr, Maghrib, Isha
        Assert.All(kinds, k => Assert.Equal(NotificationCategory.PrayerTime, k));
    }

    [Fact]
    public void BuildPlan_PreReminderScheduledBeforePrayer()
    {
        var now = TestData.At(Today, "01:00");
        var settings = DefaultSettings with { MinutesBeforePrayer = 15 };

        var plan = NotificationPlanner.BuildPlan([TestData.Day(Today)], settings, now);

        var fajrMain = plan.Single(n => n.Id == NotificationIds.ForPrayer(Today, PrayerKind.Fajr));
        var fajrPre = plan.Single(n => n.Id == NotificationIds.ForPreReminder(Today, PrayerKind.Fajr));
        Assert.Equal(fajrMain.NotifyAt.AddMinutes(-15), fajrPre.NotifyAt);
    }

    [Fact]
    public void BuildPlan_AppliesManualAdjustments()
    {
        var now = TestData.At(Today, "01:00");
        var settings = DefaultSettings with
        {
            Adjustments = new PrayerAdjustments { Fajr = 7 }
        };

        var plan = NotificationPlanner.BuildPlan([TestData.Day(Today, fajr: "04:30")], settings, now);
        var fajr = plan.Single(n => n.Id == NotificationIds.ForPrayer(Today, PrayerKind.Fajr));

        Assert.Equal(new TimeOnly(4, 37), TimeOnly.FromDateTime(fajr.NotifyAt.DateTime));
    }

    [Fact]
    public void BuildPlan_FridayNotification_OnNextFriday()
    {
        var now = TestData.At(Today, "10:00"); // Friday 10:00
        var settings = DefaultSettings with
        {
            PrayerNotificationsEnabled = false,
            FridayNotificationsEnabled = true,
            FridayNotificationTime = new TimeOnly(11, 30)
        };

        var plan = NotificationPlanner.BuildPlan([], settings, now);

        var friday = Assert.Single(plan);
        Assert.Equal(NotificationCategory.Friday, NotificationIds.GetCategory(friday.Id));
        Assert.Equal(DayOfWeek.Friday, DateOnly.FromDateTime(friday.NotifyAt.Date).DayOfWeek);
        Assert.Equal(Today, DateOnly.FromDateTime(friday.NotifyAt.Date));
    }

    [Fact]
    public void BuildPlan_FridayAfterTime_SchedulesNextWeek()
    {
        var now = TestData.At(Today, "13:00"); // Friday 13:00, after 11:30
        var settings = DefaultSettings with
        {
            PrayerNotificationsEnabled = false,
            FridayNotificationsEnabled = true,
            FridayNotificationTime = new TimeOnly(11, 30)
        };

        var plan = NotificationPlanner.BuildPlan([], settings, now);

        var friday = Assert.Single(plan);
        Assert.Equal(Today.AddDays(7), DateOnly.FromDateTime(friday.NotifyAt.Date));
    }

    [Fact]
    public void NotificationIds_AreDeterministicAndCategorized()
    {
        var id1 = NotificationIds.ForPrayer(Today, PrayerKind.Maghrib);
        var id2 = NotificationIds.ForPrayer(Today, PrayerKind.Maghrib);
        var idOtherDay = NotificationIds.ForPrayer(Today.AddDays(1), PrayerKind.Maghrib);

        Assert.Equal(id1, id2);
        Assert.NotEqual(id1, idOtherDay);
        Assert.Equal(NotificationCategory.PrayerTime, NotificationIds.GetCategory(id1));
        Assert.Equal(NotificationCategory.Verse, NotificationIds.GetCategory(NotificationIds.ForVerse(Today, 0)));
        Assert.Equal(NotificationCategory.PreReminder, NotificationIds.GetCategory(NotificationIds.ForPreReminder(Today, PrayerKind.Fajr)));
        Assert.Equal(NotificationCategory.Friday, NotificationIds.GetCategory(NotificationIds.ForFriday(Today)));
    }
}
