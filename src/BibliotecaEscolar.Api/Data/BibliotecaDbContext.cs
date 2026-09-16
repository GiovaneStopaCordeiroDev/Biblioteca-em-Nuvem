using BibliotecaEscolar.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaEscolar.Api.Data;

public class BibliotecaDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<Emprestimo> Emprestimos => Set<Emprestimo>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
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

        modelBuilder.Entity<Aluno>(entity =>
        {
            entity.ToTable("Alunos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Versao).IsConcurrencyToken();
            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Matricula).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Turma).HasMaxLength(60);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.HasIndex(x => x.Matricula).IsUnique();
            entity.HasIndex(x => x.Nome);
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
            entity.Property(x => x.Observacao).HasMaxLength(500);
            entity.HasOne(x => x.Aluno).WithMany().HasForeignKey(x => x.AlunoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Livro).WithMany().HasForeignKey(x => x.LivroId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.AlunoId, x.LivroId }).IsUnique()
                .HasFilter("\"DataDevolucao\" IS NULL AND \"CanceladoEm\" IS NULL");
            entity.HasIndex(x => x.DataPrevistaDevolucao);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios", table => table.HasCheckConstraint("CK_Usuarios_Perfil", "\"Perfil\" IN ('Administrador', 'Bibliotecario')"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Perfil).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.SupabaseAuthId).IsUnique();
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
