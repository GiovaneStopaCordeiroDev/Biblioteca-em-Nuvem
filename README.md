# Biblioteca Escolar — API

API do projeto Biblioteca em Nuvem para cadastro de livros, controle de empréstimos por nome do aluno e consulta do operador autenticado.

O backend usa ASP.NET Core, Entity Framework Core e .NET 10. O modo local utiliza SQLite com dados fictícios; o ambiente compartilhado utiliza PostgreSQL/Supabase. O bibliotecário autentica pela própria API com uma senha armazenada somente como hash PBKDF2.

## Requisitos

- SDK do .NET indicado em `global.json`.
- Para o modo local, nenhum serviço externo é necessário.
- Para PostgreSQL, uma instância compatível e as variáveis descritas em `docs/SUPABASE.md`.

## Executar localmente

```powershell
dotnet restore
dotnet run --project src/BibliotecaEscolar.Api
```

A API local fica disponível em `http://localhost:5080`; o Swagger é exposto em `/swagger` somente no ambiente de desenvolvimento. Antes do primeiro login, cadastre o único bibliotecário com o utilitário abaixo. A senha é solicitada sem aparecer no terminal:

```powershell
dotnet run --project tools/BibliotecaEscolar.PasswordTool -- bibliotecario "Nome do bibliotecário" --sqlite src/BibliotecaEscolar.Api/biblioteca-demo.db
```

Em PostgreSQL, execute o utilitário sem `--sqlite` e copie o `INSERT` gerado. Não grave senha em texto puro em SQL ou arquivos de configuração.

## Verificar a solução

```powershell
dotnet build --configuration Release
dotnet test --configuration Release
dotnet format --verify-no-changes
```

Consulte `docs/VALIDACAO.md` para as verificações de migrations, PostgreSQL e integração executadas pelo CI.

## Estrutura

```text
src/BibliotecaEscolar.Api/             API, regras, persistência e autenticação
tests/BibliotecaEscolar.Api.Tests/     testes automatizados
tools/BibliotecaEscolar.PasswordTool/  geração segura de hash e cadastro local
database/                              scripts operacionais do PostgreSQL
docs/                                  arquitetura, contrato e operação
requests/                              exemplos HTTP sem credenciais
```

## Documentação

- [Arquitetura e decisões](docs/ARQUITETURA.md)
- [Contrato da API](docs/CONTRATO-API.md)
- [Configuração do Supabase](docs/SUPABASE.md)
- [Integração da equipe](docs/INTEGRACAO-EQUIPE.md)
- [Validação](docs/VALIDACAO.md)
- [Guia de estudo](docs/GUIA-DO-ALUNO.md)

## Limites

Este repositório contém o backend. A publicação, o projeto Supabase e a integração com o frontend dependem dos ambientes da equipe. Não versione tokens, senhas, bancos locais ou arquivos privados de requisição.
