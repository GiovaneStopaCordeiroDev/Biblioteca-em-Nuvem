-- Não coloque senha em texto puro neste arquivo.
-- Gere o INSERT completo com:
-- dotnet run --project tools/BibliotecaEscolar.PasswordTool -- bibliotecario "Nome do bibliotecário"
-- O utilitário solicita e confirma a senha sem exibi-la e gera um hash PBKDF2.
-- Para o SQLite local, acrescente: --sqlite src/BibliotecaEscolar.Api/biblioteca-demo.db

-- Exemplo estrutural; substitua todos os valores entre < > pelo resultado do utilitário.
INSERT INTO public."Usuarios"
    ("Id", "Nome", "Perfil", "Ativo", "Login", "SenhaHash", "SupabaseAuthId")
VALUES
    (gen_random_uuid(), '<NOME>', 'Bibliotecario', true, '<LOGIN>', '<HASH_GERADO>', NULL);

-- A restrição IX_Usuarios_Ativo permite somente um usuário ativo.
