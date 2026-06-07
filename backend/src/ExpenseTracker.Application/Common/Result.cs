namespace ExpenseTracker.Application.Common;

public readonly record struct Error(string Code, string Message);

public sealed class Result<T>
{
    private Result(bool ok, T? value, Error? error)
    {
        IsSuccess = ok;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string code, string message) => new(false, default, new Error(code, message));
    public static Result<T> Failure(Error error) => new(false, default, error);
}

public sealed class Result
{
    private Result(bool ok, Error? error)
    {
        IsSuccess = ok;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(string code, string message) => new(false, new Error(code, message));
    public static Result Failure(Error error) => new(false, error);
}
