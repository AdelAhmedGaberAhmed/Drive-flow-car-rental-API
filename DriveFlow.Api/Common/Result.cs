namespace DriveFlow.Api.Common;

public enum ErrorKind { None, NotFound, Conflict, Invalid }

/// <summary>What a service returns: either data, or an error kind + message.</summary>
public class Result<T>
{
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public ErrorKind Kind { get; private set; } = ErrorKind.None;
    public bool Ok => Kind == ErrorKind.None;

    public static Result<T> Success(T data) => new() { Data = data };
    public static Result<T> Fail(ErrorKind kind, string error) => new() { Kind = kind, Error = error };
}
