using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace EscapePod;

[DataContract]
[StructLayout(LayoutKind.Auto)]
public readonly struct Result<TOk, TError> : IEquatable<Result<TOk, TError>>
{
    [DataMember] private readonly bool _isOk;
    [DataMember] private readonly TOk? _ok;
    [DataMember] private readonly TError? _error;

    private Result(bool isOk, TOk? ok, TError? error)
    {
        if (isOk && error is not null || !isOk && error is null)
        {
            throw new InvalidOperationException();
        }

        _isOk = isOk;
        _ok = ok;
        _error = error;
    }

    public static Result<TOk, TError> Ok(TOk ok)
        => new(true, ok, default);

    public static Result<TOk, TError> Fail(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<TOk, TError>(false, default, error);
    }

    public bool IsOk => _isOk;

    public bool IsFailure => !_isOk;

    public TOk Value => _isOk ? _ok! : throw new InvalidOperationException();

    public TError Error => _isOk ? throw new InvalidOperationException() : _error!;

    public override bool Equals(object? obj)
    {
        return obj is Result<TOk, TError> other && Equals(other);
    }

    public bool Equals(Result<TOk, TError> other)
    {
        return EqualityComparer<bool>.Default.Equals(_isOk, other._isOk)
            && EqualityComparer<TOk>.Default.Equals(_ok, other._ok)
            && EqualityComparer<TError>.Default.Equals(_error, other._error);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_isOk, _ok, _error);
    }

    public override string ToString()
    {
        return (_isOk, _ok, _error).ToString();
    }

    public static bool operator ==(Result<TOk, TError> left, Result<TOk, TError> right) =>
        left.Equals(right);

    public static bool operator !=(Result<TOk, TError> left, Result<TOk, TError> right) =>
        !(left == right);
}
