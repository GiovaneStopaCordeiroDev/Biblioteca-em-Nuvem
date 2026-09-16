# Integração e revisão do back-end

Este documento estabelece o processo de colaboração e os critérios usados na revisão técnica do backend.

## Divisão sugerida

| Área | Escopo de contribuição |
| --- | --- |
| Arquitetura/API principal | Configuração, autenticação, banco, padrões e revisão final |
| Livros | Cadastros, consultas, validações e testes de estoque |
| Alunos | Cadastros, consultas, validação de matrícula e testes |
| Empréstimos | Retirada, devolução, cancelamento, filtros e testes |
| Integração com front-end | Mapeamento de telas para DTOs, tratamento de erros e estados de carregamento |

Os nomes responsáveis devem ser preenchidos pela equipe. Combine alterações de entidades compartilhadas e migrations antes de iniciar tarefas simultâneas.

## Fluxo de trabalho

1. Cada integrante atualiza sua cópia da branch principal e cria uma branch curta para a tarefa, por exemplo `feat/filtro-livros`.
2. Confere o [contrato da API](CONTRATO-API.md) antes de alterar rotas, campos ou respostas.
3. Implementa a regra no service, o acesso ao banco em query/DbContext e mantém o controller dedicado ao HTTP.
4. Executa `dotnet format`, `dotnet build` e `dotnet test`; testa a operação afetada no Swagger.
5. Abre um pull request explicando comportamento, arquivos relevantes, testes realizados e mudanças necessárias no front-end.
6. O responsável pela integração revisa o diff, resolve divergências de contrato com os autores e incorpora a contribuição após os checks.

Não envie alterações de `bin/`, `obj/`, banco de demonstração, senhas ou arquivos locais de configuração. Variáveis de ambiente e User Secrets ficam fora do código compartilhado.

## O que revisar

- A rota continua sob `/api/v1` e recebe/devolve DTOs?
- Os campos obrigatórios, comprimentos e valores permitidos estão validados?
- A regra preserva disponibilidade, datas e vínculo entre aluno e livro?
- A operação permanece correta com duas requisições simultâneas?
- Uma atualização de livro/aluno exige e renova `versao`, sem sobrescrever escrita concorrente?
- A mutação gera auditoria na mesma transação, sem incluir dados pessoais?
- Falhas retornam `400`, `404` ou `409` de forma consistente, sem detalhes internos do banco?
- Consultas continuam paginadas e não expõem dados desnecessários?
- A autenticação real exige um usuário local ativo, sem confiar em perfil enviado pelo navegador?
- A mudança de modelo possui migrations adequadas para SQLite e PostgreSQL?
- Há teste para a regra alterada e a documentação foi atualizada quando o contrato mudou?

O workflow repete essas verificações em Linux, executa a suíte tanto em SQLite quanto em PostgreSQL 17, exige cobertura mínima de linhas, verifica migrations/SQL versionado e procura credenciais no histórico.

## Migrations em equipe

Migrations registram a evolução do banco. Não reescreva uma migration que outra pessoa já aplicou no banco compartilhado. Crie uma nova alteração para a correção. Quando duas branches alterarem modelos, integre uma de cada vez, atualize a branch seguinte e confira os snapshots de ambos os provedores.

A execução local prepara automaticamente apenas o SQLite de desenvolvimento. Aplicar migrations no Supabase é uma etapa explícita, com conferência do projeto de destino, conforme [o guia de configuração](SUPABASE.md).

## Roteiro de demonstração

Com a API local ligada, consulte livros e alunos, crie um empréstimo, confira a queda de disponibilidade, filtre por `Ativo`, devolva o livro e filtre por `Devolvido`. Experimente repetir a devolução para demonstrar o erro `409`. Cancele um lançamento de teste e confira sua retirada da listagem.

Na apresentação, explique por que aluno e usuário são entidades diferentes, por que a API controla a disponibilidade e como os DTOs combinam o trabalho do front-end e do back-end. Mostre que a revisão das contribuições é um processo previsto, a ser executado quando a equipe começar a compartilhar código.
