using System.Text.Json;
using BibliotecaEscolar.Api.Data;
using BibliotecaEscolar.Api.Models;
using BibliotecaEscolar.Api.Security;

namespace BibliotecaEscolar.Api.Services;

public sealed class AuditoriaService(
    BibliotecaDbContext db,
    ICurrentOperator currentOperator,
    BibliotecaClock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Registrar(string acao, string entidade, Guid entidadeId, object? detalhes = null)
    {
        var detalhesJson = detalhes is null ? null : JsonSerializer.Serialize(detalhes, JsonOptions);
        if (detalhesJson?.Length > 500)
            throw new InvalidOperationException("Os detalhes da auditoria excedem o limite permitido.");

        db.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            OperadorAuthId = currentOperator.AuthId,
            Acao = acao,
            Entidade = entidade,
            EntidadeId = entidadeId,
            OcorridoEm = clock.UtcNow,
            Detalhes = detalhesJson
        });
    }
}
