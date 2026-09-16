using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaEscolar.Api.Data.Migrations.Postgres;

/// <inheritdoc />
public partial class InicialPostgres : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Alunos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Matricula = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Turma = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Alunos", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Livros",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Autor = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Isbn = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                Categoria = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                QuantidadeTotal = table.Column<int>(type: "integer", nullable: false),
                QuantidadeDisponivel = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Livros", x => x.Id);
                table.CheckConstraint("CK_Livros_Estoque", "\"QuantidadeDisponivel\" >= 0 AND \"QuantidadeDisponivel\" <= \"QuantidadeTotal\"");
                table.CheckConstraint("CK_Livros_QuantidadeTotal", "\"QuantidadeTotal\" >= 1");
            });

        migrationBuilder.CreateTable(
            name: "Usuarios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SupabaseAuthId = table.Column<Guid>(type: "uuid", nullable: false),
                Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                Perfil = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Ativo = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Usuarios", x => x.Id);
                table.CheckConstraint("CK_Usuarios_Perfil", "\"Perfil\" IN ('Administrador', 'Bibliotecario')");
            });

        migrationBuilder.CreateTable(
            name: "Emprestimos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                LivroId = table.Column<Guid>(type: "uuid", nullable: false),
                DataEmprestimo = table.Column<DateOnly>(type: "date", nullable: false),
                DataPrevistaDevolucao = table.Column<DateOnly>(type: "date", nullable: false),
                DataDevolucao = table.Column<DateOnly>(type: "date", nullable: true),
                CanceladoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Emprestimos", x => x.Id);
                table.CheckConstraint("CK_Emprestimos_Devolucao", "\"DataDevolucao\" IS NULL OR \"DataDevolucao\" >= \"DataEmprestimo\"");
                table.CheckConstraint("CK_Emprestimos_Prazo", "\"DataPrevistaDevolucao\" >= \"DataEmprestimo\"");
                table.ForeignKey(
                    name: "FK_Emprestimos_Alunos_AlunoId",
                    column: x => x.AlunoId,
                    principalTable: "Alunos",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Emprestimos_Livros_LivroId",
                    column: x => x.LivroId,
                    principalTable: "Livros",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Alunos_Matricula",
            table: "Alunos",
            column: "Matricula",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Alunos_Nome",
            table: "Alunos",
            column: "Nome");

        migrationBuilder.CreateIndex(
            name: "IX_Emprestimos_AlunoId_LivroId",
            table: "Emprestimos",
            columns: new[] { "AlunoId", "LivroId" },
            unique: true,
            filter: "\"DataDevolucao\" IS NULL AND \"CanceladoEm\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Emprestimos_DataPrevistaDevolucao",
            table: "Emprestimos",
            column: "DataPrevistaDevolucao");

        migrationBuilder.CreateIndex(
            name: "IX_Emprestimos_LivroId",
            table: "Emprestimos",
            column: "LivroId");

        migrationBuilder.CreateIndex(
            name: "IX_Livros_Isbn",
            table: "Livros",
            column: "Isbn",
            unique: true,
            filter: "\"Isbn\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Livros_Titulo",
            table: "Livros",
            column: "Titulo");

        migrationBuilder.CreateIndex(
            name: "IX_Usuarios_SupabaseAuthId",
            table: "Usuarios",
            column: "SupabaseAuthId",
            unique: true);

        // O front-end acessa a API .NET. Sem políticas públicas, a Data API do
        // Supabase não pode contornar a autorização de operadores do back-end.
        migrationBuilder.Sql("""
            ALTER TABLE "Alunos" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "Livros" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "Emprestimos" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "Usuarios" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "__EFMigrationsHistory" ENABLE ROW LEVEL SECURITY;
            DO $security$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                    REVOKE ALL ON "Alunos", "Livros", "Emprestimos", "Usuarios", "__EFMigrationsHistory" FROM anon;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                    REVOKE ALL ON "Alunos", "Livros", "Emprestimos", "Usuarios", "__EFMigrationsHistory" FROM authenticated;
                END IF;
            END $security$;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Emprestimos");

        migrationBuilder.DropTable(
            name: "Usuarios");

        migrationBuilder.DropTable(
            name: "Alunos");

        migrationBuilder.DropTable(
            name: "Livros");
    }
}
