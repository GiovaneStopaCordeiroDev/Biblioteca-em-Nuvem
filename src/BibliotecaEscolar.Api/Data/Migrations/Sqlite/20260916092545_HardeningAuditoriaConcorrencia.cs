using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaEscolar.Api.Data.Migrations.Sqlite;

/// <inheritdoc />
public partial class HardeningAuditoriaConcorrencia : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "Versao",
            table: "Livros",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "Versao",
            table: "Alunos",
            type: "TEXT",
            nullable: true);

        const string NewUuidSql =
            "lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || " +
            "substr(hex(randomblob(2)), 2) || '-' || substr('89ab', abs(random()) % 4 + 1, 1) || " +
            "substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6)))";
        migrationBuilder.Sql($"UPDATE \"Livros\" SET \"Versao\" = {NewUuidSql};");
        migrationBuilder.Sql($"UPDATE \"Alunos\" SET \"Versao\" = {NewUuidSql};");

        migrationBuilder.AlterColumn<Guid>(
            name: "Versao",
            table: "Livros",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Versao",
            table: "Alunos",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "RegistrosAuditoria",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                OperadorAuthId = table.Column<Guid>(type: "TEXT", nullable: false),
                Acao = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                Entidade = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                EntidadeId = table.Column<Guid>(type: "TEXT", nullable: false),
                OcorridoEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                Detalhes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RegistrosAuditoria_Entidade_EntidadeId_OcorridoEm",
            table: "RegistrosAuditoria",
            columns: new[] { "Entidade", "EntidadeId", "OcorridoEm" });

        migrationBuilder.CreateIndex(
            name: "IX_RegistrosAuditoria_OperadorAuthId_OcorridoEm",
            table: "RegistrosAuditoria",
            columns: new[] { "OperadorAuthId", "OcorridoEm" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RegistrosAuditoria");

        migrationBuilder.DropColumn(
            name: "Versao",
            table: "Livros");

        migrationBuilder.DropColumn(
            name: "Versao",
            table: "Alunos");
    }
}
