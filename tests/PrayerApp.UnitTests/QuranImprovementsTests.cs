using Xunit;
using PrayerApp.Core.Services;
using PrayerApp.Core.Contracts;
using PrayerApp.Core.Domain;

namespace PrayerApp.UnitTests;

public class QuranImprovementsTests
{
    [Fact]
    public void NuzulOrder_Metadata_IsCorrect()
    {
        // Assert nuzul array length
        Assert.Equal(115, QuranMetadata.SurahNuzulOrder.Length);

        // Assert some key surahs
        Assert.Equal(1, QuranMetadata.SurahNuzulOrder[96]);  // Al-Alaq
        Assert.Equal(5, QuranMetadata.SurahNuzulOrder[1]);   // Al-Fatiha
        Assert.Equal(21, QuranMetadata.SurahNuzulOrder[114]); // An-Nas
        Assert.Equal(114, QuranMetadata.SurahNuzulOrder[110]); // An-Nasr
    }

    [Fact]
    public void QuranTopicProvider_ReturnsValidTopics()
    {
        var topics = QuranTopicProvider.GetTopics();

        Assert.NotEmpty(topics);
        Assert.Equal(5, topics.Count);

        foreach (var topic in topics)
        {
            Assert.False(string.IsNullOrWhiteSpace(topic.Name));
            Assert.False(string.IsNullOrWhiteSpace(topic.Icon));
            Assert.NotEmpty(topic.Verses);

            foreach (var reference in topic.Verses)
            {
                Assert.True(reference.Surah >= 1 && reference.Surah <= 114);
                Assert.True(reference.Verse >= 1);
            }
        }
    }

    [Fact]
    public void SurahInfo_NuzulNumber_IsAssigned()
    {
        var surah = new SurahInfo(1, "Al-Fatiha", "Fatiha", 7, 5);
        Assert.Equal(5, surah.NuzulNumber);
    }
}
