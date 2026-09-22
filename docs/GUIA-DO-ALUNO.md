# Guia do aluno — entender, executar e apresentar o back-end

Esta entrega é uma base funcional do back-end da Biblioteca Escolar, integrada ao front-end local. O vínculo com um projeto Supabase da equipe ainda depende das credenciais do ambiente.

## 1. O que cada atividade solicitada recebeu

| Atividade | Implementação e localização |
| --- | --- |
| Estruturar o projeto .NET | `BibliotecaEscolar.slnx`, API em `src/`, testes em `tests/`, configuração e organização em camadas |
| Configurar a conexão com Supabase | Provedor Npgsql, contexto PostgreSQL, migrations, SQL inicial e configuração por User Secrets/variáveis |
| Criar os modelos | `Livro`, `Emprestimo` e `Usuario`, com relacionamentos e restrições no banco |
| Definir os padrões da API | Rotas `/api/v1`, JSON, DTOs, paginação, validação, status HTTP e erros Problem Details |
| Revisar e integrar o back-end da equipe | Guia de contribuição, modelo de pull request, workflow de compilação/testes e convenções comuns |

A última atividade depende de contribuições futuras. Não seria correto afirmar que o código dos colegas já foi revisado sem recebê-lo. O projeto está preparado para essa revisão e integração.

## 2. Preparar o computador

Instale o SDK .NET 10, disponível na página oficial indicada no README. SDK e Runtime são diferentes: o Runtime executa aplicações prontas; o SDK também compila, restaura pacotes e executa ferramentas de desenvolvimento.

Abra a raiz deste repositório no editor e abra o terminal nessa pasta. Verifique:

```powershell
dotnet --list-sdks
```

Deve aparecer um SDK `10.0.xxx`. Depois execute:

```powershell
dotnet restore
dotnet build
dotnet run --project src/BibliotecaEscolar.Api
```

Abra `http://localhost:5080/swagger`. Para encerrar, use `Ctrl+C` no terminal. A porta 5080 e o ambiente Development vêm do arquivo `Properties/launchSettings.json`.

Na primeira execução local, as migrations criam as tabelas em um arquivo SQLite e o seed inclui exemplos fictícios semelhantes à tela. Os dados persistem entre execuções. Esta é uma alternativa para aprender e testar sem credenciais; o banco compartilhado da equipe é configurado pelo guia Supabase.

## 3. Como as partes trabalham juntas

1. O front-end envia uma requisição HTTP para uma rota.
2. O ASP.NET Core identifica o operador e verifica sua autorização.
3. O controller recebe o DTO e valida o formato da entrada.
4. O service verifica as regras da biblioteca e coordena a operação.
5. O objeto de consulta/DbContext consulta ou altera o banco.
6. A API devolve o DTO da resposta e um status HTTP.

O controller deve ser pequeno. Uma regra como “não emprestar sem exemplar disponível” pertence ao service, pois é uma regra da biblioteca. O `DbContext` representa a sessão de acesso ao banco. `EmprestimoQueries` concentra consultas e projeções específicas; cadastros simples usam o DbContext diretamente dentro do service, evitando camadas que apenas repetiriam métodos do EF.

Um DTO é um objeto usado no contrato HTTP. Ele permite receber apenas os campos que o usuário pode alterar e devolver somente os campos necessários à tela. Por exemplo, o front-end informa `quantidadeTotal`, mas não pode enviar `quantidadeDisponivel`: a disponibilidade é calculada e mantida pelo back-end.

## 4. Entidades e relacionamentos

`Livro` representa um título do acervo. Guarda título, autor, ISBN opcional, categoria opcional, total de cópias e cópias disponíveis. Nesta versão, três exemplares de Dom Casmurro são um cadastro com quantidade total igual a três. Não há ainda um código individual para cada exemplar físico.

`Emprestimo` guarda o nome que o bibliotecário digita no momento da retirada e conecta o registro ao livro por ID. Também guarda data de retirada, prazo previsto, devolução efetiva, cancelamento, observação e quantidade de renovações. Não existe cadastro separado de aluno.

`Usuario` representa o operador do sistema. Guarda nome, perfil, situação ativa e o UUID correspondente à identidade no Supabase Auth. A senha fica sob responsabilidade do serviço de autenticação; a aplicação não possui uma coluna de senha.

Os perfis `Administrador` e `Bibliotecario` podem consultar e operar o acervo. Somente `Administrador` pode excluir livros ou cancelar empréstimos. O perfil é lido da tabela `Usuarios`, nunca de um campo controlado pelo navegador. Não existe endpoint público para alguém se promover a administrador.

`RegistroAuditoria` registra cada mutação junto com o UUID do operador, sem duplicar o nome do aluno. `Livro` possui uma versão opaca usada para detectar atualizações concorrentes: o front-end precisa devolver a versão lida e tratar `409` recarregando o cadastro.

## 5. Exemplo completo de empréstimo

Suponha que um livro tenha `quantidadeTotal=2` e `quantidadeDisponivel=2`.

1. O bibliotecário digita o nome do aluno e seleciona o livro.
2. O front envia `POST /api/v1/emprestimos` com `alunoNome` e `livroId`.
3. O servidor verifica o nome, a existência do livro, o prazo e a disponibilidade.
4. Reserva uma cópia e cria o empréstimo dentro de uma transação.
5. O livro passa a ter uma cópia disponível e o empréstimo fica `Ativo`.
6. Na devolução, o front-end envia `PATCH /api/v1/emprestimos/{id}/devolucao`.
7. O servidor preenche a data efetiva, muda a resposta para `Devolvido` e devolve a disponibilidade para duas cópias.

Repetir a devolução retorna `409 Conflict`. Uma transação agrupa operações que precisam dar certo juntas: se a gravação do empréstimo falha, a reserva do exemplar também é desfeita. Atualizações condicionais no banco impedem que dois pedidos reservem a mesma última cópia.

O prazo padrão é de 14 dias. Uma data explícita pode ser enviada em `dataPrevistaDevolucao`; ela não pode anteceder o dia da retirada. O calendário usa o fuso de São Paulo. Um empréstimo ativo com prazo anterior ao dia atual tem `atrasado=true`, mas continua com status `Ativo` para manter os filtros da foto.

Um empréstimo ativo e ainda no prazo pode ser renovado até duas vezes. Cada chamada a `PATCH /api/v1/emprestimos/{id}/renovar` acrescenta 14 dias e incrementa `quantidadeRenovacoes`; tentativas além do limite, após devolução ou com atraso retornam `409`.

## 6. O que significa excluir

Excluir um empréstimo marca `CanceladoEm`, sem apagar sua linha do banco. O lançamento deixa de aparecer nas consultas comuns. Se estava ativo, o exemplar volta a ficar disponível. Se já havia sido devolvido, a exclusão não altera o estoque novamente.

Livros sem empréstimos podem ser removidos. Quando já existe histórico, a API retorna conflito para preservar os vínculos. Não foi implementada uma tela para restaurar empréstimos cancelados; isso pode ser definido como uma nova atividade.

## 7. Relacionar o código com a foto

| Componente visual | Requisição |
| --- | --- |
| Menu Livros | `GET /api/v1/livros` |
| Tabela de empréstimos | `GET /api/v1/emprestimos` |
| Pesquisa por aluno ou livro | Adicionar `?busca=Ana` |
| Filtro Ativo | Adicionar `?status=Ativo` |
| Filtro Devolvido | Adicionar `?status=Devolvido` |
| Todos os status | Omitir `status` |
| Novo empréstimo | `POST /api/v1/emprestimos` |
| Devolver | `PATCH /api/v1/emprestimos/{id}/devolucao` |
| Renovar | `PATCH /api/v1/emprestimos/{id}/renovar` |
| Excluir | `DELETE /api/v1/emprestimos/{id}` |
| Identificação do operador | `GET /api/v1/usuarios/me` |
| Resumo da tela inicial | `GET /api/v1/dashboard` |

Os campos `alunoNome`, `livroTitulo`, `dataEmprestimo`, `dataPrevistaDevolucao` e `status` alimentam as colunas da tabela. A resposta também possui o ID do livro, a data efetiva de devolução e a indicação de atraso.

## 8. Fazer a primeira chamada no front-end

Exemplo de consulta em JavaScript:

```javascript
async function listarEmprestimos({ busca = '', status = '', accessToken } = {}) {
  const params = new URLSearchParams({ page: '1', pageSize: '20' });
  if (busca.trim()) params.set('busca', busca.trim());
  if (status) params.set('status', status);

  const headers = accessToken
    ? { Authorization: `Bearer ${accessToken}` }
    : {};
  const resposta = await fetch(
    `http://localhost:5080/api/v1/emprestimos?${params}`, { headers }
  );
  const dados = await resposta.json();
  if (!resposta.ok) throw new Error(dados.detail ?? dados.title ?? 'Falha ao consultar');
  return dados; // { items, page, pageSize, totalCount, totalPages }
}
```

Esse é um exemplo para integrar ao front-end quando os arquivos estiverem disponíveis. Em Supabase, obtenha o access token pelo login do operador. Em demonstração local, ele é dispensado. Configure a URL do front-end em `Cors:AllowedOrigins` se usar outra porta.

Após criar, devolver ou excluir, atualize a lista e os contadores. No caso de `204 No Content`, não execute `resposta.json()`, pois a resposta não tem corpo. Formate datas ISO para a apresentação brasileira; evite converter uma data sem horário como se fosse um instante UTC, pois isso pode deslocar o dia no navegador.

## 9. Migrar do banco local para o Supabase

Siga [SUPABASE.md](SUPABASE.md). O processo é:

1. Criar/selecionar um projeto de desenvolvimento da equipe no Supabase.
2. Obter host, usuário e senha do PostgreSQL no painel.
3. Configurar `Database:Provider=Postgres` e a connection string no servidor.
4. Aplicar as migrations ou executar o SQL inicial entregue.
5. Configurar a URL do Supabase e autenticação por chaves assimétricas.
6. Criar a identidade do operador no Supabase Auth.
7. Executar o script de vínculo do operador na tabela `Usuarios`.
8. Testar o token no endpoint `/usuarios/me` e então testar a biblioteca.

A connection string do banco e o access token são diferentes. A primeira permite ao servidor consultar o PostgreSQL; o segundo identifica quem chamou a API. A connection string nunca vai para o front-end. A API não usa uma chave `service_role` para autenticar usuários.

As migrations PostgreSQL já habilitam RLS e revogam acesso direto de `anon`/`authenticated` às tabelas da aplicação. Assim, o front-end passa pelas regras da API. A configuração descrita usa a conexão administrativa do projeto; uma conta de banco com privilégios limitados exige grants/políticas adequados.

## 10. Evoluir o projeto em equipe

Antes de criar uma rota, combinem o método, caminho, campos e códigos de resposta. Usem uma branch por atividade e pull requests pequenos. Consultem [INTEGRACAO-EQUIPE.md](INTEGRACAO-EQUIPE.md).

Para adicionar um campo ao modelo, atualizem o modelo, o mapeamento EF, os DTOs quando necessário e as regras do service. Depois gerem migrations para os dois provedores:

```powershell
dotnet tool restore
dotnet ef migrations add NomeDaMudancaSqlite --project src/BibliotecaEscolar.Api --context SqliteBibliotecaDbContext --output-dir Data/Migrations/Sqlite
dotnet ef migrations add NomeDaMudancaPostgres --project src/BibliotecaEscolar.Api --context PostgresBibliotecaDbContext --output-dir Data/Migrations/Postgres
dotnet build
dotnet test
```

`NomeDaMudanca` deve descrever a alteração real. Revisem as migrations antes de aplicar. Para compartilhar o SQL atualizado:

```powershell
dotnet ef migrations script --idempotent --project src/BibliotecaEscolar.Api --context PostgresBibliotecaDbContext --output database/001_schema_postgres.sql
```

Não editem uma migration que já foi aplicada no banco compartilhado. Criem outra migration para evoluir o banco. Novas tabelas PostgreSQL também devem receber a configuração de RLS e permissões compatível com a arquitetura.

## 11. Roteiro curto de apresentação

1. Explique o problema: organizar o acervo e controlar empréstimos escolares.
2. Mostre as três entidades e explique que o aluno é apenas o nome registrado na retirada, enquanto o operador possui login.
3. Apresente o caminho controller → service → query/DbContext → banco.
4. Abra o Swagger e consulte os dados de demonstração.
5. Cadastre um empréstimo e mostre a redução de disponibilidade.
6. Faça a devolução e mostre a recuperação da disponibilidade.
7. Repita a devolução para demonstrar a regra de conflito.
8. Explique como o Supabase será configurado para uso compartilhado.
9. Mostre os testes e as instruções de integração da equipe.

Use o código para explicar as decisões com suas próprias palavras. Apresente as limitações da primeira versão com clareza: o projeto ainda depende dos requisitos completos, do front-end real, das credenciais da equipe e das contribuições futuras.

## 12. Problemas comuns

| Sintoma | Verificação |
| --- | --- |
| “No .NET SDKs were found” | Instalar SDK .NET 10, não apenas Runtime |
| Falha de restore | Conferir internet e acesso ao NuGet; os pacotes não estão embutidos no ZIP |
| Porta 5080 ocupada | Encerrar outra instância ou ajustar launchSettings e URL do front-end |
| CORS no navegador | Adicionar a origem exata do front-end na configuração |
| `401` no modo Supabase | Conferir token, expiração, emissor, audiência e chaves assimétricas |
| `403` no modo Supabase | Conferir UUID do Auth, perfil e situação ativa em `Usuarios` |
| Tabela não existe no PostgreSQL | Aplicar as migrations no banco correto |
| Erro de conexão Supabase | Conferir senha do banco, host/usuário do painel, TLS e modo direto/session pooler |
| Estoque retorna `409` | Ler `detail`; a operação conflitou com uma regra ou dado existente |
| Data inválida | Enviar `YYYY-MM-DD`; datas mostradas na foto não devem ser usadas como prazo fixo |

Não altere o modo de autenticação real para resolver erros de permissão em uma publicação. Corrija o token ou o vínculo do operador. O modo de demonstração existe para trabalho local com dados fictícios.
