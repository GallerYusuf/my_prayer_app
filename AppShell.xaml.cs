using Microsoft.Extensions.DependencyInjection;
using PrayerApp.Core.Contracts;
using PrayerApp.Views;

namespace PrayerApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("SurahPage", typeof(SurahPage));
		Routing.RegisterRoute("DhikrPage", typeof(DhikrPage));

		// Skip onboarding for returning users without a visible redirect.
		var settings = IPlatformApplication.Current?.Services.GetService<ISettingsStore>()?.Get();
		if (settings?.HasCompletedOnboarding == true)
			CurrentItem = MainTabBar;
	}
}
