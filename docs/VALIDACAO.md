# Validação da entrega

Verificação realizada em 05/09/2026, com .NET SDK 10.0.400 e runtime .NET 10.0.11, no Windows x64.

## Resultado

| Verificação | Resultado |
| --- | --- |
| Restauração de pacotes | Concluída |
| Compilação da solução em Release | Aprovada, zero erros e zero avisos |
| Testes automatizados em Release | 40 aprovados, zero falhas e zero ignorados |
| Modelo PostgreSQL versus migrations | Nenhuma alteração pendente |
| Modelo SQLite versus migrations | Nenhuma alteração pendente |
| Script PostgreSQL idempotente | Gerado pelo EF Core a partir das migrations |
| API HTTP em execução local | Verificada com banco temporário e dados fictícios |

## O que os testes cobrem

- 19 casos de integração HTTP: criação, consulta, filtros, paginação inválida, devolução, cancelamento, histórico, quantidade, falta de estoque, duplicidade e concorrência pela última cópia.
- 12 casos de autorização: operador ativo, perfis permitidos, identidade válida, restrições do modo demonstrativo e recusa de privilégio enviado nos metadados do token.
- 8 casos de autenticação/configuração: ausência de token, assinatura inválida, expiração, audiência incorreta, emissor incorreto, operador sem cadastro, operador autorizado e rejeição de autenticação demonstrativa em produção.
- 1 caso de busca Unicode: nome com letra acentuada em maiúscula encontrado pela busca em minúscula no SQLite.

Os testes HTTP usam bancos SQLite temporários isolados. Os testes JWT assinam tokens RS256 com chaves locais de teste; apenas a consulta externa de chaves JWKS é substituída. Não usam credenciais nem contas reais do Supabase.

## Conferência com a API iniciada

A API foi iniciada em `http://localhost:5080`, com um banco de demonstração temporário. Foram conferidos:

1. `/health/live` e `/health/ready`.
2. Swagger UI e o documento OpenAPI, incluindo o endpoint de devolução.
3. Dados iniciais com dois livros, dois alunos e dois empréstimos (um ativo e um devolvido).
4. Devolução com recuperação da quantidade disponível.
5. Repetição de devolução retornando `409`.
6. Consulta do operador demonstrativo.
7. Contadores do dashboard.
8. Resposta de CORS para o front-end local configurado.

## Como reproduzir

Na raiz do projeto, com SDK .NET 10 instalado e acesso ao NuGet:

```powershell
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
```

Para conferir as migrations:

```powershell
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project src/BibliotecaEscolar.Api --context PostgresBibliotecaDbContext
dotnet ef migrations has-pending-model-changes --project src/BibliotecaEscolar.Api --context SqliteBibliotecaDbContext
```

## Limites da validação

Não foram fornecidos projeto Supabase, credenciais ou arquivos do front-end. Portanto, não houve teste contra PostgreSQL/Supabase em execução, verificação de chaves de um projeto real, publicação nem integração com as telas reais. A geração de SQL e a conferência das migrations não substituem a execução no banco de destino. Essa validação deve ser feita pela equipe depois de seguir `SUPABASE.md`.

O pacote foi validado no Windows. O workflow fornecido prepara a mesma compilação e os testes no Linux/GitHub Actions, mas não foi executado em um repositório remoto nesta entrega. Testes aumentam a confiança nas regras verificadas; não representam garantia de ausência de todo defeito.

O computador da sessão tinha somente Runtime. Para a verificação, foi utilizado um SDK temporário, sem instalação global. Como o transporte HTTPS nativo do Windows estava indisponível nesse ambiente, a restauração usou um adaptador local com conexão externa HTTPS validada pelo Node. Esse adaptador e o SDK temporário não fazem parte da entrega: `NuGet.Config` aponta diretamente para o feed oficial. Na máquina da equipe, basta o SDK e acesso normal ao NuGet.
