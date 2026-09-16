# Conectar ao Supabase

O projeto usa o PostgreSQL do Supabase por meio do Npgsql/Entity Framework Core. O Supabase Auth identifica o operador. São configurações diferentes: a conexão restrita usada pela API, a conexão administrativa usada somente para migrations e a autenticação do usuário pelo front-end.

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
dotnet user-secrets set "Auth:Mode" "Supabase" --project src/BibliotecaEscolar.Api
dotnet user-secrets set "Auth:SupabaseUrl" "https://SEU_PROJETO.supabase.co" --project src/BibliotecaEscolar.Api
dotnet user-secrets set "Demo:SeedData" "false" --project src/BibliotecaEscolar.Api
dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:5173" --project src/BibliotecaEscolar.Api
```

Troque a origem do exemplo pela URL exata do front-end, incluindo protocolo e porta. Em `AllowedHosts`, informe apenas o host público da API, sem protocolo nem caminho; se houver mais de um, separe-os por ponto e vírgula. Curingas globais são rejeitados na inicialização. CORS define quais origens de navegador podem chamar a API; não substitui a autenticação. User Secrets evita versionar os valores, mas não é um cofre criptografado; use o gerenciador de segredos da hospedagem para produção. [User Secrets no ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).

Na hospedagem, configure variáveis de ambiente com dois sublinhados nos níveis:

| Chave da aplicação | Variável de ambiente |
| --- | --- |
| `Database:Provider` | `Database__Provider=Postgres` |
| `ConnectionStrings:Biblioteca` | `ConnectionStrings__Biblioteca=...` |
| `Auth:Mode` | `Auth__Mode=Supabase` |
| `Auth:SupabaseUrl` | `Auth__SupabaseUrl=https://SEU_PROJETO.supabase.co` |
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

## 5. Preparar a autenticação real

Este starter valida tokens por chaves públicas JWKS. Configure o projeto Supabase com uma chave de assinatura assimétrica, como ES256 ou RS256. O endpoint `https://SEU_PROJETO.supabase.co/auth/v1/.well-known/jwks.json` publica as chaves públicas; projetos que utilizam somente o segredo legado HS256 não fornecem as chaves necessárias a esse fluxo. [Validação de JWT no Supabase](https://supabase.com/docs/guides/auth/jwts).

O token aceito deve ter emissor `https://SEU_PROJETO.supabase.co/auth/v1`, audiência `authenticated`, assinatura válida e prazo de validade vigente. O front-end realiza o login pelo Supabase Auth e envia o access token no cabeçalho:

```http
Authorization: Bearer ACCESS_TOKEN_DO_USUARIO
```

Depois de criar a identidade do operador em **Authentication > Users**, copie o UUID. Abra [database/002_cadastrar_operador.sql](../database/002_cadastrar_operador.sql), substitua `UUID_DO_USUARIO_AUTH` e `NOME_DO_OPERADOR` e execute no SQL Editor. O script confirma que a identidade existe em `auth.users` e cadastra o vínculo em `public."Usuarios"`. Não redefine um operador já cadastrado. A tabela exige `Id`, `SupabaseAuthId`, `Nome`, `Perfil` (`Administrador` ou `Bibliotecario`) e `Ativo`. A API não oferece uma rota pública que permita alguém escolher o próprio perfil.

Um login válido no Supabase, isoladamente, não concede acesso aos dados da biblioteca. O registro local identifica quem pertence à equipe autorizada.

## 6. Conferir o acesso

Inicie a API e teste `GET /api/v1/usuarios/me` com o token do operador cadastrado. Depois consulte livros, alunos e empréstimos. Um token inválido deve ser recusado; uma identidade sem vínculo autorizado também não deve conseguir operar o sistema.

O cliente do front-end deve usar esta API para as tabelas da biblioteca. As migrations habilitam RLS em `Alunos`, `Livros`, `Emprestimos`, `Usuarios` e `RegistrosAuditoria`, e revogam o acesso direto das roles `anon` e `authenticated`, quando elas existem. Não são criadas políticas de acesso direto pelo navegador. A conexão Npgsql utiliza as permissões da role `biblioteca_runtime`; ela não transforma automaticamente o JWT recebido em políticas RLS do Supabase. As políticas dessa role autorizam o processo da API a acessar as linhas, enquanto as policies `Operador` e `Administrador` da aplicação decidem o que cada usuário autenticado pode fazer. [RLS e permissões no Supabase](https://supabase.com/docs/guides/database/postgres/row-level-security).

A tabela técnica `__EFMigrationsHistory` recebe a mesma proteção contra acesso direto do navegador, para preservar o controle de versões do banco.

Para voltar à demonstração local, remova apenas as substituições adicionadas com `dotnet user-secrets remove "NOME_DA_CHAVE" --project src/BibliotecaEscolar.Api` e confira os valores em `appsettings.Development.json`. Não use `clear` se houver outros segredos que a equipe ainda precisa manter.
