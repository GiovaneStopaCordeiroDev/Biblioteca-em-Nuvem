# Contrato da API

Base local: `http://localhost:5080/api/v1`. O Swagger em `/swagger` apresenta os schemas gerados pelo código e permite testar as operações no ambiente de desenvolvimento.

## Convenções

- Requisições e respostas usam JSON, campos em `camelCase` e identificadores UUID.
- Datas de empréstimo usam `YYYY-MM-DD`, por exemplo `2026-09-18`. O front-end pode exibi-las como `18/09/2026` sem mudar o formato enviado à API. A data operacional considera o fuso de São Paulo.
- Listas usam `page` (padrão 1) e `pageSize` (padrão 20), com no máximo 100 itens por página. `busca` aceita até 200 caracteres.
- Validações e falhas de negócio retornam erros padronizados no formato Problem Details.
- Exceto no login e nos health checks, envie `Authorization: Bearer <token_da_sessao>` em cada chamada.

Envelope das listagens:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

## Rotas

| Método | Rota | Finalidade |
| --- | --- | --- |
| POST | `/auth/login` | Autenticar o bibliotecário e abrir uma sessão |
| POST | `/auth/logout` | Revogar a sessão atual |
| GET | `/livros` | Listar/pesquisar livros |
| GET | `/livros/{id}` | Consultar livro |
| POST | `/livros` | Cadastrar livro |
| PUT | `/livros/{id}` | Atualizar livro |
| DELETE | `/livros/{id}` | Excluir livro sem histórico de empréstimos |
| GET | `/emprestimos` | Listar empréstimos com filtros |
| GET | `/emprestimos/{id}` | Consultar empréstimo |
| POST | `/emprestimos` | Registrar retirada |
| PATCH | `/emprestimos/{id}/devolucao` | Registrar devolução, sem corpo |
| PATCH | `/emprestimos/{id}/renovar` | Renovar por 14 dias, sem corpo |
| DELETE | `/emprestimos/{id}` | Cancelar lançamento por exclusão lógica |
| GET | `/dashboard` | Consultar resumo para a tela inicial |
| GET | `/usuarios/me` | Consultar o operador atual |

As rotas acima são relativas a `/api/v1`. Não existe cadastro público de usuários. O único bibliotecário é inserido diretamente no banco com o hash produzido por `BibliotecaEscolar.PasswordTool`. Todas as rotas de negócio exigem esse bibliotecário ativo.

O login recebe `usuario` e `senha`, retorna `accessToken`, `expiraEm` e os dados do operador. Credenciais inválidas sempre retornam a mesma resposta `401`; tentativas são limitadas por endereço. O logout retorna `204`.

## Livros

Corpo para criar livro:

```json
{
  "titulo": "Dom Casmurro",
  "autor": "Machado de Assis",
  "isbn": null,
  "categoria": "Literatura brasileira",
  "quantidadeTotal": 3
}
```

Para atualizar, envie os mesmos campos e a `versao` devolvida pela consulta mais recente:

```json
{
  "titulo": "Dom Casmurro",
  "autor": "Machado de Assis",
  "isbn": null,
  "categoria": "Literatura brasileira",
  "quantidadeTotal": 3,
  "versao": "93131d68-d5dc-4ee4-ab2e-78bcfcded609"
}
```

`titulo` e `autor` são obrigatórios. ISBN é opcional; quando informado, deve ser um ISBN-10 ou ISBN-13 válido e único. A API aceita espaços e hífens, mas devolve e armazena o valor normalizado. A quantidade é um número inteiro e deve respeitar os exemplares já emprestados. O número disponível é controlado pela API, não enviado pelo front-end.

O livro retorna uma nova `versao` depois de cada atualização e também quando uma operação de empréstimo altera o estoque. Uma versão ausente, vazia ou antiga resulta em `400` ou `409`; nesse caso, recarregue o recurso antes de permitir nova edição.

Um livro que já tenha histórico de empréstimos não pode ser excluído; a tentativa retorna `409` para preservar os vínculos históricos.

Limites de entrada para o front-end:

| Cadastro | Campos e limites |
| --- | --- |
| Livro | `titulo`: 200; `autor`: 150; `isbn`: 32; `categoria`: 80 caracteres |
| Livro | `quantidadeTotal`: inteiro entre 1 e 1.000.000 |

As respostas de livro acrescentam `id`, `quantidadeDisponivel` e `versao`.

## Empréstimos e a tela de referência

| Elemento da tela | Uso da API |
| --- | --- |
| Pesquisa por aluno ou livro | `GET /emprestimos?busca=Ana` |
| Todos os status | Omitir o parâmetro `status` |
| Ativo | `GET /emprestimos?status=Ativo` |
| Devolvido | `GET /emprestimos?status=Devolvido` |
| Atrasado | `GET /emprestimos?status=Atrasado` |
| Cancelado | `GET /emprestimos?status=Cancelado` |
| Novo empréstimo | `POST /emprestimos` |
| Devolver | `PATCH /emprestimos/{id}/devolucao` |
| Renovar | `PATCH /emprestimos/{id}/renovar` |
| Excluir | `DELETE /emprestimos/{id}` |

Também é possível filtrar por `livroId`. Os filtros podem ser combinados com paginação:

```text
/api/v1/emprestimos?busca=Ana&status=Ativo&page=1&pageSize=20
```

Para criar um empréstimo, o bibliotecário digita o nome do aluno e usa o ID retornado pela consulta de livros. Não existe cadastro de aluno:

```json
{
  "alunoNome": "Ana Beatriz",
  "livroId": "00000000-0000-0000-0000-000000000002",
  "dataPrevistaDevolucao": "2026-09-18",
  "observacao": "Entregar na biblioteca central"
}
```

O ID acima é ilustrativo, não um dado garantido do banco de demonstração. `alunoNome` é obrigatório, recebe até 150 caracteres e é armazenado no histórico exatamente como informado, após remover espaços nas extremidades. A data de retirada vem do servidor. Ao omitir `dataPrevistaDevolucao`, a API usa 14 dias após a retirada. Uma data explícita não pode anteceder a retirada; ajuste o exemplo para o dia do teste.

Na resposta, os campos `alunoNome` e `livroTitulo` preenchem as duas primeiras colunas da tabela. `dataEmprestimo` preenche Empréstimo; `dataPrevistaDevolucao`, o prazo da coluna Devolução; `dataDevolucao`, a data efetiva após a devolução. `status` é `Ativo`, `Devolvido` ou `Cancelado`, e `atrasado` permite sinalizar prazos vencidos. A resposta também inclui `quantidadeRenovacoes` e `observacao`.

A API recusa empréstimo sem exemplar disponível. Como o nome é digitado livremente e não identifica uma pessoa de forma única, pessoas homônimas não são bloqueadas. A devolução libera um exemplar; repetir a devolução gera conflito. Cada renovação acrescenta 14 dias ao prazo, até o limite de duas; empréstimos devolvidos ou atrasados não podem ser renovados. O cancelamento remove o lançamento das consultas comuns e libera o exemplar se ainda estava emprestado. Use `status=Cancelado` para consultar somente os cancelados.

## Resumo da biblioteca

`GET /dashboard` retorna `totalLivros` (cadastros de títulos), `totalExemplares`, `exemplaresDisponiveis`, `emprestimosAtivos` e `emprestimosAtrasados`. Os dois primeiros campos têm significados diferentes: um título com três cópias representa um livro cadastrado e três exemplares.

## Respostas HTTP

| Código | Significado |
| --- | --- |
| 200 | Consulta ou operação concluída com resposta |
| 201 | Cadastro criado; `Location` identifica o recurso |
| 204 | Exclusão concluída, sem corpo |
| 400 | JSON, campo, data ou filtro inválido |
| 401 | Token ausente, inválido ou expirado no modo real |
| 403 | Operador autenticado sem autorização |
| 404 | Recurso não encontrado ou empréstimo cancelado |
| 409 | Conflito com o estado atual, como falta de exemplar ou devolução repetida |

O front-end deve verificar `response.ok` antes de tratar uma resposta como sucesso. Em `204`, não tente ler um corpo JSON. Em erros, utilize `title`, `detail` e, quando presente, `errors` para orientar o usuário, mantendo os campos do formulário para correção.
