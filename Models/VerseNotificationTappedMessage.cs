using CommunityToolkit.Mvvm.Messaging.Messages;

namespace PrayerApp.Models;

/// <summary>
/// Bir ayet bildirimine dokunulduğunda ilgili ayeti uygulamaya taşır.
/// </summary>
public sealed class VerseNotificationTappedMessage(QuranVerse verse)
    : ValueChangedMessage<QuranVerse>(verse);
