using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaEscolar.Api.Data.Migrations.Postgres;

/// <inheritdoc />
public partial class AutenticacaoBibliotecario : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Usuarios_Perfil",
            table: "Usuarios");

        migrationBuilder.AlterColumn<Guid>(
            name: "SupabaseAuthId",
            table: "Usuarios",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<string>(
            name: "Login",
            table: "Usuarios",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SenhaHash",
            table: "Usuarios",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "SessoesUsuarios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CriadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ExpiraEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                RevogadaEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SessoesUsuarios", x => x.Id);
                table.ForeignKey(
                    name: "FK_SessoesUsuarios_Usuarios_UsuarioId",
                    column: x => x.UsuarioId,
                    principalTable: "Usuarios",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql("""
            ALTER TABLE "SessoesUsuarios" ENABLE ROW LEVEL SECURITY;
            DO $security$
            BEGIN
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon') THEN
                    REVOKE ALL ON "SessoesUsuarios" FROM anon;
                END IF;
                IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated') THEN
                    REVOKE ALL ON "SessoesUsuarios" FROM authenticated;
                END IF;
            END
            $security$;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_Usuarios_Ativo",
            table: "Usuarios",
            column: "Ativo",
            unique: true,
            filter: "\"Ativo\" = TRUE");

        migrationBuilder.CreateIndex(
            name: "IX_Usuarios_Login",
            table: "Usuarios",
            column: "Login",
            unique: true,
            filter: "\"Login\" IS NOT NULL");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Usuarios_Perfil",
            table: "Usuarios",
            sql: "\"Perfil\" = 'Bibliotecario'");

        migrationBuilder.CreateIndex(
            name: "IX_SessoesUsuarios_ExpiraEm",
            table: "SessoesUsuarios",
            column: "ExpiraEm");

        migrationBuilder.CreateIndex(
            name: "IX_SessoesUsuarios_TokenHash",
            table: "SessoesUsuarios",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SessoesUsuarios_UsuarioId",
            table: "SessoesUsuarios",
            column: "UsuarioId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SessoesUsuarios");

        migrationBuilder.DropIndex(
            name: "IX_Usuarios_Ativo",
            table: "Usuarios");

        migrationBuilder.DropIndex(
            name: "IX_Usuarios_Login",
            table: "Usuarios");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Usuarios_Perfil",
            table: "Usuarios");

        migrationBuilder.DropColumn(
            name: "Login",
            table: "Usuarios");

        migrationBuilder.DropColumn(
            name: "SenhaHash",
            table: "Usuarios");

        migrationBuilder.AlterColumn<Guid>(
            name: "SupabaseAuthId",
            table: "Usuarios",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Usuarios_Perfil",
            table: "Usuarios",
            sql: "\"Perfil\" IN ('Administrador', 'Bibliotecario')");
    }
}
