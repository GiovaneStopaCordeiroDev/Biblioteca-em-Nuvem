-- Execute no SQL Editor do seu projeto Supabase DEPOIS das migrations.
-- Primeiro crie a conta em Authentication > Users e copie o UUID.
-- Substitua UUID_DO_USUARIO_AUTH e NOME_DO_OPERADOR antes de executar.
-- O script falha se o UUID não corresponder a uma conta existente no Auth.
DO $$
DECLARE
    auth_id uuid := 'UUID_DO_USUARIO_AUTH';
BEGIN
    IF NOT EXISTS (SELECT 1 FROM auth.users WHERE id = auth_id) THEN
        RAISE EXCEPTION 'Usuário não encontrado no Supabase Auth';
    END IF;

    INSERT INTO public."Usuarios" ("Id", "SupabaseAuthId", "Nome", "Perfil", "Ativo")
    VALUES (gen_random_uuid(), auth_id, 'NOME_DO_OPERADOR', 'Administrador', true)
    ON CONFLICT ("SupabaseAuthId") DO NOTHING;
END $$;

-- Para outro operador, repita usando seu UUID. O perfil também pode ser Bibliotecario.
-- Este script não redefine o perfil nem reativa silenciosamente operadores existentes.
