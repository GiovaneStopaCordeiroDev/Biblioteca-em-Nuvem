-- Execute como proprietário do banco, DEPOIS das migrations.
-- Esta role é exclusiva da API em execução: ela não pode aplicar migrations.
-- O script não define senha. Configure-a fora deste arquivo depois da execução.

DO $role$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'biblioteca_runtime') THEN
        CREATE ROLE biblioteca_runtime
            LOGIN
            NOSUPERUSER
            NOCREATEDB
            NOCREATEROLE
            NOINHERIT
            NOREPLICATION
            NOBYPASSRLS
            CONNECTION LIMIT 20;
    END IF;
END
$role$;

ALTER ROLE biblioteca_runtime
    LOGIN
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOINHERIT
    NOREPLICATION
    NOBYPASSRLS
    CONNECTION LIMIT 20;
ALTER ROLE biblioteca_runtime SET statement_timeout = '15s';
ALTER ROLE biblioteca_runtime SET idle_in_transaction_session_timeout = '15s';

-- PUBLIC não deve conceder privilégios que contornem os grants explícitos abaixo.
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE ALL PRIVILEGES ON TABLE
    public."Alunos",
    public."Livros",
    public."Emprestimos",
    public."Usuarios",
    public."RegistrosAuditoria",
    public."__EFMigrationsHistory"
FROM PUBLIC;

REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM biblioteca_runtime;
REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM biblioteca_runtime;
REVOKE CREATE ON SCHEMA public FROM biblioteca_runtime;
GRANT USAGE ON SCHEMA public TO biblioteca_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    public."Alunos",
    public."Livros",
    public."Emprestimos"
TO biblioteca_runtime;
GRANT SELECT ON TABLE public."Usuarios" TO biblioteca_runtime;
GRANT INSERT ON TABLE public."RegistrosAuditoria" TO biblioteca_runtime;

-- Como as tabelas usam RLS, a role de serviço recebe políticas apenas para as
-- operações que a API precisa. A autorização do usuário continua na API.
DO $policies$
DECLARE
    table_name text;
BEGIN
    FOREACH table_name IN ARRAY ARRAY['Alunos', 'Livros', 'Emprestimos']
    LOOP
        IF to_regclass(format('public.%I', table_name)) IS NULL THEN
            RAISE EXCEPTION 'Tabela public.% não encontrada; aplique as migrations primeiro', table_name;
        END IF;

        IF NOT EXISTS (
            SELECT 1
            FROM pg_policies
            WHERE schemaname = 'public'
              AND tablename = table_name
              AND policyname = 'biblioteca_runtime_crud'
        ) THEN
            EXECUTE format(
                'CREATE POLICY biblioteca_runtime_crud ON public.%I TO biblioteca_runtime USING (true) WITH CHECK (true)',
                table_name);
        END IF;
    END LOOP;

    IF to_regclass('public."Usuarios"') IS NULL THEN
        RAISE EXCEPTION 'Tabela public.Usuarios não encontrada; aplique as migrations primeiro';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_policies
        WHERE schemaname = 'public'
          AND tablename = 'Usuarios'
          AND policyname = 'biblioteca_runtime_read'
    ) THEN
        CREATE POLICY biblioteca_runtime_read
            ON public."Usuarios"
            FOR SELECT
            TO biblioteca_runtime
            USING (true);
    END IF;

    IF to_regclass('public."RegistrosAuditoria"') IS NULL THEN
        RAISE EXCEPTION 'Tabela public.RegistrosAuditoria não encontrada; aplique as migrations primeiro';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_policies
        WHERE schemaname = 'public'
          AND tablename = 'RegistrosAuditoria'
          AND policyname = 'biblioteca_runtime_insert'
    ) THEN
        CREATE POLICY biblioteca_runtime_insert
            ON public."RegistrosAuditoria"
            FOR INSERT
            TO biblioteca_runtime
            WITH CHECK (true);
    END IF;
END
$policies$;

-- Confirme os grants antes de configurar a aplicação. O resultado esperado não
-- inclui a tabela __EFMigrationsHistory nem privilégios CREATE no schema.
SELECT table_name, privilege_type
FROM information_schema.role_table_grants
WHERE grantee = 'biblioteca_runtime'
ORDER BY table_name, privilege_type;
