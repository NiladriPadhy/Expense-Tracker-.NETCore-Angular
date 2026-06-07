using System.Net.Mime;
using System.Text.Json;
using ExpenseTracker.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Middleware;

public sealed class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger, IHostEnvironment env)
    {
        _next = next; _logger = logger; _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ValidationAppException vex)
        {
            await WriteProblem(context, vex.StatusCode, vex.Code, vex.Message, vex.Errors).ConfigureAwait(false);
        }
        catch (AppException aex)
        {
            await WriteProblem(context, aex.StatusCode, aex.Code, aex.Message, null).ConfigureAwait(false);
        }
        catch (FluentValidation.ValidationException fvex)
        {
            var errs = fvex.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await WriteProblem(context, 400, "validation_failed", "Validation failed.", errs).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            await WriteProblem(context, 401, "unauthorized", "Authentication required.", null).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
#pragma warning disable CA1848 // LoggerMessage delegates not warranted for a single fallback path
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
#pragma warning restore CA1848
            var detail = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            await WriteProblem(context, 500, "internal_error", detail, null).ConfigureAwait(false);
        }
    }

    private static Task WriteProblem(HttpContext ctx, int status, string code, string title, IReadOnlyDictionary<string, string[]>? errors)
    {
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        var pd = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://expensetracker.local/errors/{code}",
        };
        pd.Extensions["code"] = code;
        if (errors is not null) pd.Extensions["errors"] = errors;
        var correlationId = ctx.Items.TryGetValue("CorrelationId", out var c) ? c?.ToString() : null;
        if (!string.IsNullOrEmpty(correlationId))
        {
            pd.Extensions["correlationId"] = correlationId;
        }
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(pd));
    }
}
