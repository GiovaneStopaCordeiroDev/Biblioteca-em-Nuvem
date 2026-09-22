using BibliotecaEscolar.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Data;

public class BibliotecaDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<Emprestimo> Emprestimos => Set<Emprestimo>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<SessaoUsuario> SessoesUsuarios => Set<SessaoUsuario>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Livro>(entity =>
        {
            entity.ToTable("Livros", table =>
            {
                table.HasCheckConstraint("CK_Livros_QuantidadeTotal", "\"QuantidadeTotal\" >= 1");
                table.HasCheckConstraint("CK_Livros_Estoque", "\"QuantidadeDisponivel\" >= 0 AND \"QuantidadeDisponivel\" <= \"QuantidadeTotal\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Versao).IsConcurrencyToken();
            entity.Property(x => x.Titulo).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Autor).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Isbn).HasMaxLength(32);
            entity.Property(x => x.Categoria).HasMaxLength(80);
            entity.HasIndex(x => x.Isbn).IsUnique().HasFilter("\"Isbn\" IS NOT NULL");
            entity.HasIndex(x => x.Titulo);
        });

        modelBuilder.Entity<Emprestimo>(entity =>
        {
            entity.ToTable("Emprestimos", table =>
            {
                table.HasCheckConstraint("CK_Emprestimos_Prazo", "\"DataPrevistaDevolucao\" >= \"DataEmprestimo\"");
                table.HasCheckConstraint("CK_Emprestimos_Devolucao", "\"DataDevolucao\" IS NULL OR \"DataDevolucao\" >= \"DataEmprestimo\"");
                table.HasCheckConstraint("CK_Emprestimos_Renovacoes", "\"QuantidadeRenovacoes\" BETWEEN 0 AND 2");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AlunoNome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Observacao).HasMaxLength(500);
            entity.HasOne(x => x.Livro).WithMany().HasForeignKey(x => x.LivroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.AlunoNome);
            entity.HasIndex(x => x.DataPrevistaDevolucao);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios", table => table.HasCheckConstraint("CK_Usuarios_Perfil", "\"Perfil\" = 'Bibliotecario'"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Perfil).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Login).HasMaxLength(80);
            entity.Property(x => x.SenhaHash).HasMaxLength(500);
            entity.HasIndex(x => x.SupabaseAuthId).IsUnique();
            entity.HasIndex(x => x.Login).IsUnique().HasFilter("\"Login\" IS NOT NULL");
            entity.HasIndex(x => x.Ativo).IsUnique().HasFilter("\"Ativo\" = TRUE");
        });

        modelBuilder.Entity<SessaoUsuario>(entity =>
        {
            entity.ToTable("SessoesUsuarios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => x.ExpiraEm);
            entity.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegistroAuditoria>(entity =>
        {
            entity.ToTable("RegistrosAuditoria");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Acao).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Entidade).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Detalhes).HasMaxLength(500);
            entity.HasIndex(x => new { x.Entidade, x.EntidadeId, x.OcorridoEm });
            entity.HasIndex(x => new { x.OperadorAuthId, x.OcorridoEm });
        });
    }
}
