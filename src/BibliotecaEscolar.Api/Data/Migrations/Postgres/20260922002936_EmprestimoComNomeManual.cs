using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaEscolar.Api.Data.Migrations.Postgres;

/// <inheritdoc />
public partial class EmprestimoComNomeManual : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Emprestimos_Alunos_AlunoId",
            table: "Emprestimos");

        migrationBuilder.DropIndex(
            name: "IX_Emprestimos_AlunoId_LivroId",
            table: "Emprestimos");

        migrationBuilder.AddColumn<string>(
            name: "AlunoNome",
            table: "Emprestimos",
            type: "character varying(150)",
            maxLength: 150,
            nullable: false,
            defaultValue: "");

        migrationBuilder.DropColumn(
            name: "AlunoId",
            table: "Emprestimos");

        migrationBuilder.DropTable(
            name: "Alunos");

        migrationBuilder.CreateIndex(
            name: "IX_Emprestimos_AlunoNome",
            table: "Emprestimos",
            column: "AlunoNome");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Emprestimos_AlunoNome",
            table: "Emprestimos");

        migrationBuilder.DropColumn(
            name: "AlunoNome",
            table: "Emprestimos");

        migrationBuilder.AddColumn<Guid>(
            name: "AlunoId",
            table: "Emprestimos",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        migrationBuilder.CreateTable(
            name: "Alunos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                Matricula = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Turma = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                Versao = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Alunos", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Emprestimos_AlunoId_LivroId",
            table: "Emprestimos",
            columns: new[] { "AlunoId", "LivroId" },
            unique: true,
            filter: "\"DataDevolucao\" IS NULL AND \"CanceladoEm\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Alunos_Matricula",
            table: "Alunos",
            column: "Matricula",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Alunos_Nome",
            table: "Alunos",
            column: "Nome");

        migrationBuilder.AddForeignKey(
            name: "FK_Emprestimos_Alunos_AlunoId",
            table: "Emprestimos",
            column: "AlunoId",
            principalTable: "Alunos",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
