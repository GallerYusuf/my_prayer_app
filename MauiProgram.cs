using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using PrayerApp.Core.Common;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Services;
using PrayerApp.Infrastructure.Database;
using PrayerApp.Infrastructure.Providers;
using PrayerApp.Infrastructure.Repositories;
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
						Id = LocalNotificationScheduler.ChannelPrayer,
						Name = "Namaz Vakti",
						Description = "Namaz vakti girdiğinde bildirim",
						Importance = AndroidImportance.High
					});
					android.AddChannel(new AndroidNotificationChannelRequest
					{
						Id = LocalNotificationScheduler.ChannelPreReminder,
						Name = "Vakit Öncesi Hatırlatma",
						Description = "Namaz vaktinden önce hatırlatma",
						Importance = AndroidImportance.High
					});
					android.AddChannel(new AndroidNotificationChannelRequest
					{
						Id = LocalNotificationScheduler.ChannelVerse,
						Name = "Günün Ayeti",
						Description = "Günlük ayet bildirimleri",
						Importance = AndroidImportance.Default
					});
					android.AddChannel(new AndroidNotificationChannelRequest
					{
						Id = LocalNotificationScheduler.ChannelOther,
						Name = "Diğer Hatırlatmalar",
						Description = "Cuma ve diğer manevi hatırlatmalar",
						Importance = AndroidImportance.Default
					});
				});
			})
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("AmiriQuran.ttf", "AmiriQuran");
			});

		RegisterCoreServices(builder.Services);
		RegisterViewModels(builder.Services);

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void RegisterCoreServices(IServiceCollection services)
	{
		services.AddSingleton<IClock, SystemClock>();
		services.AddSingleton<ISettingsStore, SettingsStore>();
		services.AddSingleton<IKeyValueStore, PreferencesKeyValueStore>();

		// SQLite database in app data; created lazily off the UI thread.
		services.AddSingleton(_ => new AppDatabase(
			Path.Combine(FileSystem.AppDataDirectory, "prayerapp.db3")));

		services.AddHttpClient<IPrayerTimeProvider, AladhanPrayerTimeProvider>(client =>
		{
			client.BaseAddress = new Uri("https://api.aladhan.com/v1/");
			client.Timeout = TimeSpan.FromSeconds(30);
			client.DefaultRequestHeaders.UserAgent.ParseAdd("EzanVakti/1.0");
		})
		.ConfigurePrimaryHttpMessageHandler(HttpClientPlatformHelper.CreateHandler);

		services.AddSingleton<IPrayerDayRepository, PrayerDayRepository>();
		services.AddSingleton<IPrayerScheduleService, PrayerScheduleService>();

		services.AddSingleton<IQuranRepository>(sp => new QuranRepository(
			sp.GetRequiredService<AppDatabase>(),
			async _ => await FileSystem.OpenAppPackageFileAsync("quran_tr.json"),
			sp.GetRequiredService<IClock>(),
			sp.GetRequiredService<ILogger<QuranRepository>>()));

		services.AddSingleton<IDhikrRepository, DhikrRepository>();
		services.AddSingleton<IKhatmRepository, KhatmRepository>();

		services.AddSingleton<INotificationScheduler, LocalNotificationScheduler>();
		services.AddSingleton<INotificationCoordinator, NotificationCoordinator>();
#if ANDROID
		services.AddSingleton<IPersistentNotificationService, Platforms.Android.PersistentNotificationService>();
		services.AddSingleton<IAudioPlayer, Platforms.Android.AudioPlayer>();
#elif IOS || MACCATALYST
		services.AddSingleton<IPersistentNotificationService, NullPersistentNotificationService>();
		services.AddSingleton<IAudioPlayer, Platforms.iOS.AudioPlayer>();
#else
		services.AddSingleton<IPersistentNotificationService, NullPersistentNotificationService>();
		services.AddSingleton<IAudioPlayer, NullAudioPlayer>();
#endif

		services.AddSingleton<ILocationService, LocationService>();
		services.AddSingleton<IPrayerTrackingService, PrayerTrackingService>();
	}

	private static void RegisterViewModels(IServiceCollection services)
	{
		services.AddTransient<MainViewModel>();
		services.AddTransient<CalendarViewModel>();
		services.AddTransient<QiblaViewModel>();
		services.AddTransient<QuranViewModel>();
		services.AddTransient<SurahViewModel>();
		services.AddTransient<DhikrViewModel>();
		services.AddTransient<SettingsViewModel>();
		services.AddTransient<OnboardingViewModel>();
		services.AddTransient<EsmaulHusnaViewModel>();
		services.AddTransient<DuasViewModel>();
		services.AddTransient<ReligiousDaysViewModel>();
		services.AddTransient<KazaViewModel>();
		services.AddTransient<ZakatViewModel>();

		services.AddTransient<MainPage>();
		services.AddTransient<CalendarPage>();
		services.AddTransient<QiblaPage>();
		services.AddTransient<QuranPage>();
		services.AddTransient<SurahPage>();
		services.AddTransient<DhikrPage>();
		services.AddTransient<SettingsPage>();
		services.AddTransient<OnboardingPage>();
		services.AddTransient<EsmaulHusnaPage>();
		services.AddTransient<DuasPage>();
		services.AddTransient<ReligiousDaysPage>();
		services.AddTransient<KazaPage>();
		services.AddTransient<ZakatPage>();
	}
}
