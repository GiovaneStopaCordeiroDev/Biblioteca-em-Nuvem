using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaEscolar.Api.Data.Migrations.Postgres;

/// <inheritdoc />
public partial class RenovacaoEmprestimos : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Observacao",
            table: "Emprestimos",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "QuantidadeRenovacoes",
            table: "Emprestimos",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Emprestimos_Renovacoes",
            table: "Emprestimos",
            sql: "\"QuantidadeRenovacoes\" BETWEEN 0 AND 2");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Emprestimos_Renovacoes",
            table: "Emprestimos");

        migrationBuilder.DropColumn(
            name: "Observacao",
            table: "Emprestimos");

        migrationBuilder.DropColumn(
            name: "QuantidadeRenovacoes",
            table: "Emprestimos");
    }
}
