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
        var (status, detail) = exception switch
        {
            CredenciaisInvalidasException invalidCredentials =>
                (StatusCodes.Status401Unauthorized, invalidCredentials.Message),
            RequisicaoInvalidaException invalidRequest =>
                (StatusCodes.Status400BadRequest, invalidRequest.Message),
            RecursoNaoEncontradoException notFound =>
                (StatusCodes.Status404NotFound, notFound.Message),
            ConflitoDeDominioException conflict =>
                (StatusCodes.Status409Conflict, conflict.Message),
            _ => MapUnexpectedException(exception, httpContext)
        };

        httpContext.Response.StatusCode = status;
        await Results.Problem(new ProblemDetails
        {
            Status = status,
            Title = status switch { 400 => "Dados inválidos", 401 => "Não autorizado", 404 => "Registro não encontrado", 409 => "Conflito", _ => "Erro interno" },
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }).ExecuteAsync(httpContext);
        return true;
    }

    private (int Status, string Detail) MapUnexpectedException(Exception exception, HttpContext httpContext)
    {
        var databaseError = exception is DbUpdateException update ? update.InnerException : exception;
        if (databaseError is PostgresException { SqlState: "23505" or "23503" or "23514" or "40001" or "40P01" }
            || databaseError is SqliteException { SqliteErrorCode: 19 or 5 or 6 })
        {
            return (
                StatusCodes.Status409Conflict,
                "A operação conflita com os dados atuais. Verifique duplicidades, vínculos e disponibilidade e tente novamente.");
        }

        logger.LogError(exception, "Erro na API. TraceId: {TraceId}", httpContext.TraceIdentifier);
        return (
            StatusCodes.Status500InternalServerError,
            "Ocorreu um erro interno. Informe o traceId à equipe de back-end.");
    }
}
