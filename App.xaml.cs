using Microsoft.Extensions.DependencyInjection;
using PrayerApp.Services.Interfaces;

namespace PrayerApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Dark;
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
			var prayerTimes = await prayerTimeService.GetTodayPrayerTimesAsync(settings.City, settings.Country);
			await notificationService.RescheduleIfStaleAsync(settings, prayerTimes);
		}
		catch
		{
			// Foreground loading already surfaces user-facing prayer time errors.
		}
	}
}