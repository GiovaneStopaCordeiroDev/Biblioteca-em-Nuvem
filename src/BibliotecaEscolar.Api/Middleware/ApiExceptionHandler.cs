using BibliotecaEscolar.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BibliotecaEscolar.Api.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var status = StatusCodes.Status500InternalServerError;
        var detail = "Ocorreu um erro interno. Informe o traceId à equipe de back-end.";
        if (exception is ApiException api)
        {
            status = api.StatusCode;
            detail = api.Message;
        }
        else
        {
            var databaseError = exception is DbUpdateException update ? update.InnerException : exception;
            if (databaseError is PostgresException { SqlState: "23505" or "23503" or "23514" or "40001" or "40P01" }
                || databaseError is SqliteException { SqliteErrorCode: 19 or 5 or 6 })
            {
                status = StatusCodes.Status409Conflict;
                detail = "A operação conflita com os dados atuais. Verifique duplicidades, vínculos e disponibilidade e tente novamente.";
            }
            else logger.LogError(exception, "Erro na API. TraceId: {TraceId}", httpContext.TraceIdentifier);
        }
        httpContext.Response.StatusCode = status;
        await Results.Problem(new ProblemDetails
        {
            Status = status,
            Title = status switch { 400 => "Dados inválidos", 404 => "Registro não encontrado", 409 => "Conflito", _ => "Erro interno" },
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }).ExecuteAsync(httpContext);
        return true;
    }
}
