using PrayerApp.Models;

namespace PrayerApp.Services.Interfaces;

public interface IVerseService
{
    Task InitializeAsync();
    QuranVerse GetRandomVerse();
    string GetRandomVerseText();
}
