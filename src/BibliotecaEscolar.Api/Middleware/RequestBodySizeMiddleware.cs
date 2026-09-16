using BibliotecaEscolar.Api.Configuration;
using Microsoft.AspNetCore.Http.Features;

namespace BibliotecaEscolar.Api.Middleware;

public sealed class RequestBodySizeMiddleware(
    RequestDelegate next,
    RequestSecurityOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false }
            && (sizeFeature.MaxRequestBodySize is null
                || sizeFeature.MaxRequestBodySize > options.MaxRequestBodySizeBytes))
            sizeFeature.MaxRequestBodySize = options.MaxRequestBodySizeBytes;

        if (context.Request.ContentLength > options.MaxRequestBodySizeBytes)
        {
            await Results.Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Corpo da requisição muito grande",
                detail: $"O corpo da requisição deve ter no máximo {options.MaxRequestBodySizeBytes} bytes.")
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}
