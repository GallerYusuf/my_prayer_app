using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using PrayerApp.Services;
using PrayerApp.Services.Interfaces;
using PrayerApp.ViewModels;
using PrayerApp.Views;

namespace PrayerApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseLocalNotification(config =>
			{
				config.AddAndroid(android =>
				{
					android.AddChannel(new AndroidNotificationChannelRequest
					{
						Id = "prayer_app_channel",
						Name = "Ezan Vakti Bildirimleri",
						Description = "Namaz vakti ve ayet bildirimleri",
						Importance = AndroidImportance.High
					});
				});
			})
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddHttpClient<IPrayerTimeService, PrayerTimeService>(client =>
		{
			client.BaseAddress = new Uri("https://api.aladhan.com/v1/");
			client.Timeout = TimeSpan.FromSeconds(30);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("EzanVakti/1.0");
		})
		.ConfigurePrimaryHttpMessageHandler(HttpClientPlatformHelper.CreateHandler);

		builder.Services.AddSingleton<IVerseService, VerseService>();
		builder.Services.AddSingleton<ISettingsService, SettingsService>();
		builder.Services.AddSingleton<INotificationSchedulerService, NotificationSchedulerService>();

		builder.Services.AddSingleton<IWidgetDataService, WidgetDataService>();
		builder.Services.AddSingleton<IQiblaService, QiblaService>();
		builder.Services.AddSingleton<IPrayerTrackingService, PrayerTrackingService>();
		builder.Services.AddSingleton<IRamadanService, RamadanService>();

		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<QiblaViewModel>();

		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<QiblaPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
