using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace EscapePod;

[DataContract]
[StructLayout(LayoutKind.Auto)]
public readonly record struct Result<TOk, TError>
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
}
