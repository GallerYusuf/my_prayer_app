using Microsoft.Extensions.DependencyInjection;

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
			services?.GetService<Services.Interfaces.INotificationSchedulerService>()?.StartForegroundMonitor();
		};
		return window;
	}
}