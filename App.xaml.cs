using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using PrayerApp.Models;
using PrayerApp.Services;
using PrayerApp.Services.Interfaces;

namespace PrayerApp;

public partial class App : Application
{
	public static QuranVerse? PendingNotificationVerse { get; private set; }

	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Dark;

		LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
		window.Created += (_, _) =>
		{
			var services = IPlatformApplication.Current?.Services;
			services?.GetService<INotificationSchedulerService>()?.StartForegroundMonitor();
		};
		window.Resumed += async (_, _) => await RefreshNotificationsIfNeededAsync();
		return window;
	}

	private static async Task RefreshNotificationsIfNeededAsync()
	{
		var services = IPlatformApplication.Current?.Services;
		var settingsService = services?.GetService<ISettingsService>();
		var prayerTimeService = services?.GetService<IPrayerTimeService>();
		var notificationService = services?.GetService<INotificationSchedulerService>();

		if (settingsService is null || prayerTimeService is null || notificationService is null)
			return;

		var settings = settingsService.GetSettings();
		if (!settings.HasCompletedOnboarding)
			return;

		try
		{
			var result = await prayerTimeService.GetTodayPrayerTimesAsync(settings.LocationQuery, settings.Country);
            await notificationService.RescheduleIfStaleAsync(settings, result.Times);
		}
		catch
		{
			// Foreground loading already surfaces user-facing prayer time errors.
		}
	}

	private void OnNotificationActionTapped(NotificationActionEventArgs e)
	{
		if (e.IsDismissed)
			return;

		var verse = NotificationSchedulerService.ResolveTappedVerse(e.Request.NotificationId, e.Request.ReturningData);
		if (verse is null)
			return;

		PendingNotificationVerse = verse;

		MainThread.BeginInvokeOnMainThread(async () =>
		{
			WeakReferenceMessenger.Default.Send(new VerseNotificationTappedMessage(verse));

			if (Shell.Current is not null)
				await Shell.Current.GoToAsync("//MainPage");
		});
	}

	public static QuranVerse? ConsumePendingNotificationVerse()
	{
		var verse = PendingNotificationVerse;
		PendingNotificationVerse = null;
		return verse;
	}
}
