using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ProductApi.Domain.Exceptions;
using ProductApi.Api.Dtos;

namespace ProductApi.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ProductNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found");
            await WriteAsync(context, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (DuplicateSkuException ex)
        {
            logger.LogWarning(ex, "Duplicate SKU");
            await WriteAsync(context, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (InvalidProductException ex)
        {
            logger.LogWarning(ex, "Domain rule violation");
            await WriteAsync(context, StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled internal error");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "Internal Server Error", "An internal server error occurred");
        }
    }

    private static Task WriteAsync(HttpContext context, int status, string error, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;
        var body = new ErrorResponseDto(DateTimeOffset.UtcNow.ToString("O"), status, error, message);
        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}
