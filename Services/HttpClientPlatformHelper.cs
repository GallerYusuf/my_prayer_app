using System.Net;

namespace PrayerApp.Services;

public static class HttpClientPlatformHelper
{
    public static HttpMessageHandler CreateHandler()
    {
#if ANDROID
        return new Xamarin.Android.Net.AndroidMessageHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
#elif IOS || MACCATALYST
        return new NSUrlSessionHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
#else
        return new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
#endif
    }
}
