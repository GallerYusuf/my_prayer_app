using System.Diagnostics.CodeAnalysis;

namespace PrayerApp.Core.Common;

/// <summary>Lightweight result type: either a value or an <see cref="AppError"/>.</summary>
public readonly struct Result<T>
{
    private readonly T? _value;

    public AppError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public T? Value => _value;

    private Result(T value)
    {
        _value = value;
        Error = null;
    }

    private Result(AppError error)
    {
        _value = default;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(AppError error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(AppError error) => Failure(error);

    public T GetValueOrThrow() =>
        IsSuccess ? _value! : throw new InvalidOperationException($"Result failed: {Error!.Message}");
}
