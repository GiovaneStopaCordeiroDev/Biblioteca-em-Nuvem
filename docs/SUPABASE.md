# Conectar ao Supabase

O projeto usa somente o PostgreSQL do Supabase por meio do Npgsql/Entity Framework Core. A autenticação pertence à API; o Supabase Auth não é necessário. A conexão restrita da API e a conexão administrativa usada para migrations continuam sendo credenciais diferentes.

Esta entrega não contém credenciais e não altera um projeto remoto. É possível começar pelo SQLite local e seguir este guia quando a equipe tiver um projeto Supabase.

## 1. Obter a conexão do banco

No painel do projeto Supabase, abra **Connect** e copie os dados da conexão PostgreSQL. Para um servidor persistente, use a conexão direta; em uma rede somente IPv4, utilize o **Session pooler**, normalmente na porta `5432`. Host e nome de usuário variam conforme a opção: copie ambos do painel. [Conexões PostgreSQL no Supabase](https://supabase.com/docs/guides/database/connecting-to-postgres).

O Npgsql recebe uma string no formato abaixo. A credencial copiada do painel tem privilégios elevados e deve ser usada somente para preparar o banco e aplicar migrations:

```text
Host=HOST_DO_PAINEL;Port=5432;Database=postgres;Username=USUARIO_DO_PAINEL;Password=SENHA_DO_BANCO;SSL Mode=VerifyFull
```

`VerifyFull` valida o certificado e o nome do servidor. Se o certificado exigir uma raiz específica, obtenha-a no painel e configure `Root Certificate=CAMINHO_DO_CERTIFICADO`. [Segurança e TLS no Npgsql](https://www.npgsql.org/doc/security.html).

A senha acima é a do banco PostgreSQL, não uma chave `anon`, `publishable` ou `service_role`. Nunca coloque nenhuma dessas conexões no front-end. Depois das migrations, a API deve usar a role limitada criada na etapa 4.

## 2. Configurar a API

Na raiz do projeto, use User Secrets para testar a conexão em `Development`:

```powershell
dotnet user-secrets set "Database:Provider" "Postgres" --project src/BibliotecaEscolar.Api
dotnet user-secrets set "ConnectionStrings:Biblioteca" "Host=HOST_DO_PAINEL;Port=5432;Database=postgres;Username=biblioteca_runtime;Password=SENHA_DA_ROLE_RUNTIME;SSL Mode=VerifyFull" --project src/BibliotecaEscolar.Api
dotnet user-secrets set "Auth:Mode" "Database" --project src/BibliotecaEscolar.Api
dotnet user-secrets set "Auth:SessionHours" "8" --project src/BibliotecaEscolar.Api
dotnet user-secrets set "Demo:SeedData" "false" --project src/BibliotecaEscolar.Api
dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:5173" --project src/BibliotecaEscolar.Api
```

Troque a origem do exemplo pela URL exata do front-end, incluindo protocolo e porta. Em `AllowedHosts`, informe apenas o host público da API, sem protocolo nem caminho; se houver mais de um, separe-os por ponto e vírgula. Curingas globais são rejeitados na inicialização. CORS define quais origens de navegador podem chamar a API; não substitui a autenticação. User Secrets evita versionar os valores, mas não é um cofre criptografado; use o gerenciador de segredos da hospedagem para produção. [User Secrets no ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).

Na hospedagem, configure variáveis de ambiente com dois sublinhados nos níveis:

| Chave da aplicação | Variável de ambiente |
| --- | --- |
| `Database:Provider` | `Database__Provider=Postgres` |
| `ConnectionStrings:Biblioteca` | `ConnectionStrings__Biblioteca=...` |
| `Auth:Mode` | `Auth__Mode=Database` |
| `Auth:SessionHours` | `Auth__SessionHours=8` |
| `Demo:SeedData` | `Demo__SeedData=false` |
| `Cors:AllowedOrigins:0` | `Cors__AllowedOrigins__0=https://SEU_FRONTEND` |
| `AllowedHosts` | `AllowedHosts=api.exemplo.com` |
| `RequestSecurity:MaxRequestBodySizeBytes` | `RequestSecurity__MaxRequestBodySizeBytes=1048576` |

## 3. Aplicar as migrations

O PostgreSQL não recebe migrations automaticamente ao iniciar a API. A factory usada pelo comando EF lê a conexão de uma variável de ambiente, de forma independente da configuração da demonstração. Mesmo que você já tenha cadastrado a conexão em User Secrets para executar a API, defina a variável abaixo no terminal em que rodará a migration.

Na raiz, substitua os marcadores pela conexão administrativa, confira a conexão de destino e execute:

```powershell
$env:ConnectionStrings__Biblioteca = 'Host=HOST_DO_PAINEL;Port=5432;Database=postgres;Username=USUARIO_DO_PAINEL;Password=SENHA_DO_BANCO;SSL Mode=VerifyFull'
dotnet tool restore
dotnet ef database update --project src/BibliotecaEscolar.Api --context PostgresBibliotecaDbContext
```

Esse comando altera o banco apontado pela variável. Use um projeto de desenvolvimento da equipe na primeira execução. O seed fictício local não é uma carga de dados para o banco compartilhado. A variável vale para esse terminal e é herdada pelos processos iniciados nele. Remova-a logo depois com `Remove-Item Env:ConnectionStrings__Biblioteca`; não execute a API nesse terminal com a credencial administrativa.

Como alternativa ao comando, abra [database/001_schema_postgres.sql](../database/001_schema_postgres.sql) e execute seu conteúdo no SQL Editor do Supabase. O script é gerado pelas mesmas migrations e registra o histórico do EF. Escolha um dos caminhos; não é necessário executar ambos. O script inicial é idempotente: uma migration já registrada não é reaplicada. Para mudanças futuras, gere um novo script de migrations.

## 4. Criar a role restrita da API

Depois das migrations, execute [database/003_configurar_role_runtime.sql](../database/003_configurar_role_runtime.sql) como proprietário do banco. O script cria `biblioteca_runtime` sem senha, concede leitura/escrita nas tabelas operacionais, leitura em `Usuarios`, somente inserção em `RegistrosAuditoria` e nenhum acesso ao histórico de migrations ou a DDL. Ele também cria as políticas RLS necessárias para essa role de serviço.

Defina uma senha forte fora do repositório. Com `psql`, prefira o comando interativo abaixo, que solicita a senha sem gravá-la no arquivo SQL:

```text
\password biblioteca_runtime
```

Se o provedor exigir `ALTER ROLE`, execute-o diretamente no console administrativo e não salve o comando com a senha em arquivo, histórico compartilhado ou captura de tela. Guarde a senha no gerenciador de segredos da hospedagem. Em seguida, use `Username=biblioteca_runtime` na conexão da API configurada na etapa 2.

A role de runtime não consegue aplicar migrations por projeto. Mantenha a credencial proprietária separada e disponibilize-a apenas no job ou terminal que executa `dotnet ef database update`. Se uma migration futura criar uma tabela, revise o script de grants e as políticas RLS antes de publicar a versão da API que usa essa tabela.

## 5. Cadastrar o bibliotecário

Gere o hash localmente; a senha não deve aparecer no arquivo SQL, no Git ou no painel de hospedagem:

```powershell
dotnet run --project tools/BibliotecaEscolar.PasswordTool -- bibliotecario "Nome do bibliotecário"
```

O utilitário solicita e confirma uma senha de pelo menos 12 caracteres e imprime um `INSERT` com hash PBKDF2. Execute esse `INSERT` no SQL Editor depois das migrations. A restrição do banco permite somente um usuário ativo. A API não possui cadastro público nem recuperação de senha.

O front envia as credenciais somente a `POST /api/v1/auth/login` por HTTPS. A resposta contém um token aleatório temporário; o banco guarda somente o SHA-256 desse token. `POST /api/v1/auth/logout` revoga a sessão.

## 6. Conferir o acesso

Inicie a API, faça login e teste `GET /api/v1/usuarios/me` com o token retornado. Depois consulte livros e empréstimos. Senha incorreta e token inválido devem ser recusados.

O cliente do front-end deve usar esta API para as tabelas da biblioteca. As migrations habilitam RLS em `Livros`, `Emprestimos`, `Usuarios`, `SessoesUsuarios` e `RegistrosAuditoria`, e revogam o acesso direto das roles `anon` e `authenticated`, quando elas existem. Não são criadas políticas de acesso direto pelo navegador. A role `biblioteca_runtime` recebe somente os grants e políticas necessários ao processo da API. [RLS e permissões no Supabase](https://supabase.com/docs/guides/database/postgres/row-level-security).

A tabela técnica `__EFMigrationsHistory` recebe a mesma proteção contra acesso direto do navegador, para preservar o controle de versões do banco.

Para voltar à demonstração local, remova apenas as substituições adicionadas com `dotnet user-secrets remove "NOME_DA_CHAVE" --project src/BibliotecaEscolar.Api` e confira os valores em `appsettings.Development.json`. Não use `clear` se houver outros segredos que a equipe ainda precisa manter.
