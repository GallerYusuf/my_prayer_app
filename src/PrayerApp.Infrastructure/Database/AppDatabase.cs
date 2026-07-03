using SQLite;

namespace PrayerApp.Infrastructure.Database;

/// <summary>
/// Single async SQLite connection for the app, with lightweight versioned migrations.
/// The database path is injected so tests can use a temp file.
/// </summary>
public sealed class AppDatabase : IAsyncDisposable
{
    public const int CurrentSchemaVersion = 1;

    private readonly SQLiteAsyncConnection _connection;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public AppDatabase(string databasePath)
    {
        _connection = new SQLiteAsyncConnection(
            databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_initialized)
            return _connection;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
                return _connection;

            await MigrateAsync();
            _initialized = true;
            return _connection;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task MigrateAsync()
    {
        var version = await _connection.ExecuteScalarAsync<int>("PRAGMA user_version");

        if (version < 1)
        {
            await _connection.CreateTableAsync<Entities.PrayerDayRow>();
            await _connection.CreateTableAsync<Entities.CacheMetaRow>();
            await _connection.CreateTableAsync<Entities.SurahRow>();
            await _connection.CreateTableAsync<Entities.VerseRow>();
            await _connection.CreateTableAsync<Entities.FavoriteVerseRow>();
            await _connection.CreateTableAsync<Entities.DhikrSessionRow>();
        }

        if (version != CurrentSchemaVersion)
            await _connection.ExecuteAsync($"PRAGMA user_version = {CurrentSchemaVersion}");
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.CloseAsync();
        _initLock.Dispose();
    }
}
