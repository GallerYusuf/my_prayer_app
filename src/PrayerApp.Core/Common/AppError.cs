namespace PrayerApp.Core.Common;

/// <summary>Application-level error categories used to map failures to user-friendly messages.</summary>
public enum ErrorKind
{
    Unknown = 0,
    Network,
    Timeout,
    NotFound,
    InvalidData,
    PermissionDenied,
    Storage,
    StaleCache,
    NoCache
}

/// <summary>Typed error carrying a category and a user-presentable Turkish message.</summary>
public sealed record AppError(ErrorKind Kind, string Message)
{
    public static AppError Network(string message = "İnternet bağlantısı kurulamadı. Bağlantınızı kontrol edip tekrar deneyin.")
        => new(ErrorKind.Network, message);

    public static AppError Timeout(string message = "Sunucu yanıt vermedi. Lütfen daha sonra tekrar deneyin.")
        => new(ErrorKind.Timeout, message);

    public static AppError NotFound(string message = "Aradığınız veri bulunamadı.")
        => new(ErrorKind.NotFound, message);

    public static AppError InvalidData(string message = "Veri beklenen formatta gelmedi. Lütfen daha sonra tekrar deneyin.")
        => new(ErrorKind.InvalidData, message);

    public static AppError PermissionDenied(string message = "Bu işlem için gerekli izin verilmedi.")
        => new(ErrorKind.PermissionDenied, message);

    public static AppError Storage(string message = "Yerel veri deposuna erişilemedi.")
        => new(ErrorKind.Storage, message);

    public static AppError NoCache(string message = "Henüz kaydedilmiş veri yok. İnternet bağlantısıyla ilk verileri indirin.")
        => new(ErrorKind.NoCache, message);

    public static AppError Unknown(string message = "Beklenmeyen bir sorun oluştu. Lütfen tekrar deneyin.")
        => new(ErrorKind.Unknown, message);
}
