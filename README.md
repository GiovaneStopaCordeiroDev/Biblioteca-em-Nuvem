# Biblioteca Escolar — back-end inicial

API para o projeto de Análise e Desenvolvimento de Sistemas, baseada na imagem da tela de empréstimos. Inclui livros, alunos, empréstimos, consulta do operador autenticado e um resumo para a tela inicial. Esta é a base do back-end; a integração com o front-end real e a revisão do código dos colegas serão feitas quando esses arquivos estiverem disponíveis.

O projeto usa ASP.NET Core, Entity Framework Core e .NET 10, uma versão LTS. [Política de suporte da Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy).

## Começar no computador

1. Instale o [SDK do .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0). O SDK é necessário para compilar; somente o Runtime não basta.
2. Abra esta pasta no VS Code ou no Visual Studio e abra um terminal na raiz `BibliotecaEscolar.Backend`.
3. Execute:

```powershell
dotnet restore
dotnet run --project src/BibliotecaEscolar.Api
```

4. Acesse [Swagger local](http://localhost:5080/swagger). Abra um endpoint, clique em **Try it out** e depois em **Execute**.
5. Comece pelos `GET` de livros, alunos e empréstimos. Use os IDs devolvidos para testar um novo empréstimo e sua devolução.

O perfil local usa o ambiente `Development`, um arquivo SQLite persistente e dados fictícios. As tabelas e os exemplos são preparados ao iniciar nesse modo. Não é preciso ter conta no Supabase para experimentar. O operador de demonstração é autenticado automaticamente; esse modo é recusado fora de `Development` e deve ser usado apenas no computador de desenvolvimento.

Para compilar e executar os testes:

```powershell
dotnet build
dotnet test
```

Encerre a API com `Ctrl+C`. As alterações feitas no banco local permanecem na próxima execução.

## O que já está organizado

| Atividade da equipe | Entrega nesta base |
| --- | --- |
| Estruturar o projeto .NET | API com camadas, injeção de dependência e projeto de testes |
| Configurar conexão com Supabase | Provedor PostgreSQL, configuração externa e migrations próprias |
| Criar modelos | `Livro`, `Aluno`, `Emprestimo` e `Usuario` |
| Definir padrões da API | `/api/v1`, DTOs, paginação, validações e erros padronizados |
| Revisar e integrar contribuições | Guia de trabalho e critérios de revisão; código dos colegas ainda não recebido |

O Supabase está preparado no código, mas a conexão real depende de um projeto e das credenciais da equipe. Nenhum banco remoto é criado ou alterado ao executar a demonstração local.

## Documentação

- [Arquitetura e decisões](docs/ARQUITETURA.md)
- [Contrato da API e ligação com a tela](docs/CONTRATO-API.md)
- [Configuração do Supabase](docs/SUPABASE.md)
- [Como integrar o código da equipe](docs/INTEGRACAO-EQUIPE.md)
- [Guia detalhado para estudar e apresentar](docs/GUIA-DO-ALUNO.md)
- [Exemplos de requisições HTTP](requests/BibliotecaEscolar.http)
- [Relatório de validação e limites dos testes](docs/VALIDACAO.md)

As regras que a foto não mostra foram adotadas como decisões iniciais: prazo de 14 dias, um empréstimo por aluno/livro ativo e controle de exemplares por quantidade. A equipe pode ajustá-las conforme a orientação do professor.

Se a pasta for usada como raiz de um repositório GitHub, o workflow em `.github/workflows/backend.yml` compila e testa os pull requests. Se ela for colocada dentro de outro projeto, ajuste o diretório de execução do workflow.
