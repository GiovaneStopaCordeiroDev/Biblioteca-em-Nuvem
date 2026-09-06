CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE TABLE "Alunos" (
        "Id" uuid NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Matricula" character varying(40) NOT NULL,
        "Turma" character varying(60),
        "Email" character varying(254),
        CONSTRAINT "PK_Alunos" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE TABLE "Livros" (
        "Id" uuid NOT NULL,
        "Titulo" character varying(200) NOT NULL,
        "Autor" character varying(150) NOT NULL,
        "Isbn" character varying(32),
        "Categoria" character varying(80),
        "QuantidadeTotal" integer NOT NULL,
        "QuantidadeDisponivel" integer NOT NULL,
        CONSTRAINT "PK_Livros" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Livros_Estoque" CHECK ("QuantidadeDisponivel" >= 0 AND "QuantidadeDisponivel" <= "QuantidadeTotal"),
        CONSTRAINT "CK_Livros_QuantidadeTotal" CHECK ("QuantidadeTotal" >= 1)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE TABLE "Usuarios" (
        "Id" uuid NOT NULL,
        "SupabaseAuthId" uuid NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Perfil" character varying(30) NOT NULL,
        "Ativo" boolean NOT NULL,
        CONSTRAINT "PK_Usuarios" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Usuarios_Perfil" CHECK ("Perfil" IN ('Administrador', 'Bibliotecario'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE TABLE "Emprestimos" (
        "Id" uuid NOT NULL,
        "AlunoId" uuid NOT NULL,
        "LivroId" uuid NOT NULL,
        "DataEmprestimo" date NOT NULL,
        "DataPrevistaDevolucao" date NOT NULL,
        "DataDevolucao" date,
        "CanceladoEm" timestamp with time zone,
        CONSTRAINT "PK_Emprestimos" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Emprestimos_Devolucao" CHECK ("DataDevolucao" IS NULL OR "DataDevolucao" >= "DataEmprestimo"),
        CONSTRAINT "CK_Emprestimos_Prazo" CHECK ("DataPrevistaDevolucao" >= "DataEmprestimo"),
        CONSTRAINT "FK_Emprestimos_Alunos_AlunoId" FOREIGN KEY ("AlunoId") REFERENCES "Alunos" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_Emprestimos_Livros_LivroId" FOREIGN KEY ("LivroId") REFERENCES "Livros" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE UNIQUE INDEX "IX_Alunos_Matricula" ON "Alunos" ("Matricula");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE INDEX "IX_Alunos_Nome" ON "Alunos" ("Nome");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE UNIQUE INDEX "IX_Emprestimos_AlunoId_LivroId" ON "Emprestimos" ("AlunoId", "LivroId") WHERE "DataDevolucao" IS NULL AND "CanceladoEm" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE INDEX "IX_Emprestimos_DataPrevistaDevolucao" ON "Emprestimos" ("DataPrevistaDevolucao");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE INDEX "IX_Emprestimos_LivroId" ON "Emprestimos" ("LivroId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE UNIQUE INDEX "IX_Livros_Isbn" ON "Livros" ("Isbn") WHERE "Isbn" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE INDEX "IX_Livros_Titulo" ON "Livros" ("Titulo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    CREATE UNIQUE INDEX "IX_Usuarios_SupabaseAuthId" ON "Usuarios" ("SupabaseAuthId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260905221221_InicialPostgres') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260905221221_InicialPostgres', '10.0.11');
    END IF;
END $EF$;
COMMIT;

