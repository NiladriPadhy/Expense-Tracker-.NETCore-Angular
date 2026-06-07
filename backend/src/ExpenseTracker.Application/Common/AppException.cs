namespace ExpenseTracker.Application.Common;

public class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public AppException(string code, string message, int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string code, string message) : base(code, message, 404) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string code, string message) : base(code, message, 409) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string code, string message) : base(code, message, 403) { }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string code, string message) : base(code, message, 401) { }
}

public sealed class ValidationAppException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IReadOnlyDictionary<string, string[]> errors)
        : base("validation_failed", "One or more validation errors occurred.", 400)
    {
        Errors = errors;
    }
}
