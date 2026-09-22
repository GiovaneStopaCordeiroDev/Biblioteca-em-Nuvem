using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BibliotecaEscolar.Api.Data.Migrations.Sqlite;

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
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "TEXT");

        migrationBuilder.AddColumn<string>(
            name: "Login",
            table: "Usuarios",
            type: "TEXT",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SenhaHash",
            table: "Usuarios",
            type: "TEXT",
            maxLength: 500,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "SessoesUsuarios",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                UsuarioId = table.Column<Guid>(type: "TEXT", nullable: false),
                TokenHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                CriadaEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                ExpiraEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                RevogadaEm = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
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
            type: "TEXT",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
            oldClrType: typeof(Guid),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Usuarios_Perfil",
            table: "Usuarios",
            sql: "\"Perfil\" IN ('Administrador', 'Bibliotecario')");
    }
}
