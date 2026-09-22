# Validação da entrega

Verificação realizada em 16/09/2026, no Windows x64, com .NET SDK 10.0.401, runtime .NET 10.0.12, SQLite e PostgreSQL 17.9.

## Resultado

| Verificação | Resultado |
| --- | --- |
| Restauração de pacotes | Concluída |
| Compilação da solução em Release | Aprovada, zero erros e zero avisos |
| Formatação | `dotnet format --verify-no-changes` aprovado |
| Testes em SQLite | 54 aprovados, zero falhas e zero ignorados |
| Testes em PostgreSQL | Deve ser reexecutado no ambiente com PostgreSQL 17 após esta alteração |
| Cobertura de linhas da API | 85,20%; mínimo do CI: 75% |
| Modelo PostgreSQL versus migrations | Nenhuma alteração pendente |
| Modelo SQLite versus migrations | Nenhuma alteração pendente |
| Script PostgreSQL idempotente | Reproduzido exatamente a partir das migrations |
| Role de runtime e políticas RLS | Script aplicado com sucesso em banco descartável e revertido |
| Auditoria de dependências | Nenhum pacote vulnerável nas fontes atuais |
| Atualizações de dependências | Nenhum pacote direto desatualizado nas fontes atuais |

## O que os testes cobrem

- Operações HTTP de livros, empréstimos, dashboard, paginação, filtros, validação e Problem Details.
- Nome manual na retirada, estoque, histórico, renovação, devolução/cancelamento e concorrência pela última cópia.
- Concorrência otimista de livros, com recusa de versões antigas sem sobrescrever dados.
- Auditoria atômica das mutações, identificação do operador e ausência de dados pessoais nos detalhes.
- Autenticação JWT, autorização de operador, separação do perfil Administrador e restrição do modo demonstrativo.
- Health checks, busca Unicode em SQLite e execução das mesmas regras no PostgreSQL real.

Os testes HTTP usam bancos temporários isolados. Em SQLite, cada factory cria um arquivo descartável. Em PostgreSQL, cada factory cria um schema exclusivo, aplica as migrations e o remove ao terminar. Os testes JWT assinam tokens RS256 com chaves locais de teste e não usam credenciais do Supabase.

## Como reproduzir

Na raiz do projeto, com o SDK indicado em `global.json` e acesso ao NuGet:

```powershell
dotnet restore BibliotecaEscolar.slnx
dotnet build BibliotecaEscolar.slnx --no-restore --configuration Release
dotnet format BibliotecaEscolar.slnx --no-restore --verify-no-changes
dotnet test BibliotecaEscolar.slnx --no-build --configuration Release
```

Para executar contra PostgreSQL, informe uma conexão administrativa para um banco de testes. Os schemas criados recebem o prefixo `biblioteca_test_` e são descartados automaticamente:

```powershell
$env:BIBLIOTECA_TEST_POSTGRES = 'Host=localhost;Port=5432;Database=biblioteca_tests;Username=postgres;Password=SENHA'
dotnet test BibliotecaEscolar.slnx --no-build --configuration Release
Remove-Item Env:BIBLIOTECA_TEST_POSTGRES
```

Para conferir as migrations:

```powershell
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project src/BibliotecaEscolar.Api --context PostgresBibliotecaDbContext
dotnet ef migrations has-pending-model-changes --project src/BibliotecaEscolar.Api --context SqliteBibliotecaDbContext
```

## Limites da validação

Não foram fornecidos projeto Supabase, credenciais, hospedagem nem frontend. Portanto, não houve publicação, validação das chaves de um projeto real ou integração com as telas reais. PostgreSQL/Npgsql e as migrations foram exercitados localmente; a conexão, RLS e configuração final ainda precisam ser validadas no projeto Supabase da equipe.

O workflow prepara as mesmas verificações em Linux/GitHub Actions, incluindo busca de segredos no histórico, cobertura mínima e os dois provedores de banco. Ele só será executado remotamente depois que a branch for enviada ao GitHub e um push ou pull request disparar o workflow.
