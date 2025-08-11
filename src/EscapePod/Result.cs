using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace EscapePod;

[DataContract]
[StructLayout(LayoutKind.Auto)]
public readonly struct Result : IEquatable<Result>
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
        => new(false, error);

    // Convenience methods for the other Result types.

    public static Result<TOk> Ok<TOk>(TOk ok)
        => Result<TOk>.Ok(ok);

    public static Result<TOk?> Fail<TOk>(string error)
        => Result<TOk>.Fail(error);

    public static Result<TOk, TError?> Ok<TOk, TError>(TOk ok)
        => Result<TOk, TError>.Ok(ok);

    public static Result<TOk?, TError> Fail<TOk, TError>(TError error)
        => Result<TOk, TError>.Fail(error);

    public bool IsOk => _isOk;

    public bool IsFailure => !_isOk;

    public string Error => _isOk ? throw new InvalidOperationException() : _error!;

    public override bool Equals(object? obj)
    {
        return obj is Result other && Equals(other);
    }

    public bool Equals(Result other)
    {
        return EqualityComparer<bool>.Default.Equals(_isOk, other._isOk)
            && EqualityComparer<string>.Default.Equals(_error, other._error);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_isOk, _error);
    }

    public override string ToString()
    {
        return (_isOk, _error).ToString();
    }

    public static bool operator ==(Result left, Result right) =>
        left.Equals(right);

    public static bool operator !=(Result left, Result right) =>
        !(left == right);
}
