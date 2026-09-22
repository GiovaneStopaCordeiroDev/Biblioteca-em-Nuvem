using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaEscolar.Api.Data.Migrations.Postgres;

/// <inheritdoc />
public partial class HardeningAuditoriaConcorrencia : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "Versao",
            table: "Livros",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "Versao",
            table: "Alunos",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql("UPDATE \"Livros\" SET \"Versao\" = gen_random_uuid();");
        migrationBuilder.Sql("UPDATE \"Alunos\" SET \"Versao\" = gen_random_uuid();");

        migrationBuilder.AlterColumn<Guid>(
            name: "Versao",
            table: "Livros",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AlterColumn<Guid>(
            name: "Versao",
            table: "Alunos",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "RegistrosAuditoria",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OperadorAuthId = table.Column<Guid>(type: "uuid", nullable: false),
                Acao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Entidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                EntidadeId = table.Column<Guid>(type: "uuid", nullable: false),
                OcorridoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Detalhes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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

        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        migrationBuilder.Sql("""
            DO $indexes$
            DECLARE
                trgm_schema text;
            BEGIN
                SELECT quote_ident(namespace.nspname)
                INTO trgm_schema
                FROM pg_extension extension
                JOIN pg_namespace namespace ON namespace.oid = extension.extnamespace
                WHERE extension.extname = 'pg_trgm';

                IF trgm_schema IS NULL THEN
                    RAISE EXCEPTION 'A extensão pg_trgm não está disponível';
                END IF;

                EXECUTE format('CREATE INDEX "IX_Livros_Titulo_Trgm" ON "Livros" USING gin ("Titulo" %s.gin_trgm_ops)', trgm_schema);
                EXECUTE format('CREATE INDEX "IX_Livros_Autor_Trgm" ON "Livros" USING gin ("Autor" %s.gin_trgm_ops)', trgm_schema);
                EXECUTE format('CREATE INDEX "IX_Alunos_Nome_Trgm" ON "Alunos" USING gin ("Nome" %s.gin_trgm_ops)', trgm_schema);
            END $indexes$;
            """);
        migrationBuilder.Sql("""
            ALTER TABLE "RegistrosAuditoria" ENABLE ROW LEVEL SECURITY;
            DO $security$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                    REVOKE ALL ON "RegistrosAuditoria" FROM anon;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                    REVOKE ALL ON "RegistrosAuditoria" FROM authenticated;
                END IF;
            END $security$;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Alunos_Nome_Trgm\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Livros_Autor_Trgm\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Livros_Titulo_Trgm\";");

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
