using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Services;
using PrayerApp.Services;

namespace PrayerApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		var settings = IPlatformApplication.Current?.Services.GetService<ISettingsStore>()?.Get();
		ThemeManager.Apply(settings?.Theme ?? Core.Domain.ThemePreference.System);

		LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
		window.Resumed += (_, _) => RescheduleInBackground();
		return window;
	}

	private static void RescheduleInBackground()
	{
		var services = IPlatformApplication.Current?.Services;
		var coordinator = services?.GetService<INotificationCoordinator>();
		if (coordinator is null)
			return;

		_ = Task.Run(async () =>
		{
			try
			{
				await coordinator.RescheduleIfStaleAsync();
			}
			catch (Exception ex)
			{
				services?.GetService<ILogger<App>>()?.LogError(ex, "Resume reschedule failed");
			}
		});
	}

	private void OnNotificationActionTapped(NotificationActionEventArgs e)
	{
		if (e.IsDismissed)
			return;

		// Verse notifications carry a "surah:verse" payload; open the verse in context.
		var payload = e.Request.ReturningData;
		if (string.IsNullOrWhiteSpace(payload))
			return;

		var parts = payload.Split(':');
		if (parts.Length != 2 || !int.TryParse(parts[0], out var surah) || !int.TryParse(parts[1], out var verse))
			return;

		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				if (Shell.Current is not null)
					await Shell.Current.GoToAsync($"//QuranPage/SurahPage?surah={surah}&verse={verse}");
			}
			catch (Exception ex)
			{
				IPlatformApplication.Current?.Services
					.GetService<ILogger<App>>()?.LogError(ex, "Verse notification navigation failed");
			}
		});
	}
}
