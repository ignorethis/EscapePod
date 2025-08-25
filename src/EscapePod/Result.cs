using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace EscapePod;

[DataContract]
[StructLayout(LayoutKind.Auto)]
public readonly record struct Result
{
    [DataMember] private readonly bool _isOk;
    [DataMember] private readonly string? _error;

    private Result(bool isOk, string? error)
    {
        if (isOk && error is not null || !isOk && error is null)
        {
            throw new InvalidOperationException();
        }

        _isOk = isOk;
        _error = error;
    }

    public static Result Ok()
        => new(true, null);

    public static Result Fail(string error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(false, error);
    }

    // Convenience methods for the other Result types.

    public static Result<TOk> Ok<TOk>(TOk ok)
        => Result<TOk>.Ok(ok);

    public static Result<TOk> Fail<TOk>(string error)
        => Result<TOk>.Fail(error);

    public static Result<TOk, TError> Ok<TOk, TError>(TOk ok)
        => Result<TOk, TError>.Ok(ok);

    public static Result<TOk, TError> Fail<TOk, TError>(TError error)
        => Result<TOk, TError>.Fail(error);

    public bool IsOk => _isOk;

    public bool IsFailure => !_isOk;

    public string Error => _isOk ? throw new InvalidOperationException() : _error!;
}
