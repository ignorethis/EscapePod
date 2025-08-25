using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace EscapePod;

[DataContract]
[StructLayout(LayoutKind.Auto)]
public readonly record struct Result<TOk>
{
    [DataMember] private readonly bool _isOk;
    [DataMember] private readonly TOk? _ok;
    [DataMember] private readonly string? _error;

    private Result(bool isOk, TOk? ok, string? error)
    {
        if (isOk && error is not null || !isOk && error is null)
        {
            throw new InvalidOperationException();
        }

        _isOk = isOk;
        _ok = ok;
        _error = error;
    }

    public static Result<TOk> Ok(TOk ok)
        => new(true, ok, null);

    public static Result<TOk> Fail(string error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<TOk>(false, default, error);
    }

    public bool IsOk => _isOk;

    public bool IsFailure => !_isOk;

    public TOk Value => _isOk ? _ok! : throw new InvalidOperationException();

    public string Error => _isOk ? throw new InvalidOperationException() : _error!;
}
