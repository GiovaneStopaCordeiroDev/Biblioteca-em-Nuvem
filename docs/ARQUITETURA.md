# Arquitetura e decisões iniciais

## Escopo observado e hipóteses

A referência mostra os menus Início, Livros, Alunos e Empréstimos. Na tela de empréstimos aparecem pesquisa por aluno ou livro, filtro Ativo/Devolvido, criação, devolução e exclusão. Esses elementos orientam a primeira versão da API.

A imagem não define login, campos de cadastro, exemplares individuais nem regras escolares. Esta base assume uma biblioteca, operadores com acesso ao acervo, prazo padrão de 14 dias e exemplares controlados por quantidade. Configurações, notificações e ajuda podem ser especificadas em uma etapa futura.

## Organização do código

```text
./
├── src/BibliotecaEscolar.Api/
│   ├── Controllers/      # HTTP: rotas, status e entrada/saída
│   ├── DTOs/             # Contratos enviados e recebidos
│   ├── Services/         # Regras e operações do sistema
│   ├── Queries/          # Consultas e projeções especializadas
│   ├── Models/           # Entidades persistidas
│   ├── Data/             # DbContext, migrations e dados de demonstração
│   ├── Configuration/    # Opções de banco, autenticação e CORS
│   ├── Security/         # Identificação e autorização do operador
│   └── Middleware/       # Tratamento transversal de requisições
├── tests/BibliotecaEscolar.Api.Tests/
└── docs/
```

Uma requisição chega ao controller, passa pelo service e utiliza objetos de consulta ou o DbContext para consultar e alterar o banco. Os DTOs definem o que o front-end recebe, sem expor diretamente as entidades do Entity Framework.

As camadas ficam em um único projeto de API para facilitar o aprendizado e a integração entre os alunos. Não há necessidade de criar vários serviços independentes para este escopo.

## Modelo de dados

| Entidade | Responsabilidade | Relações principais |
| --- | --- | --- |
| `Livro` | Título, autor, ISBN opcional, categoria e quantidade de exemplares | Um livro possui vários empréstimos |
| `Aluno` | Nome, matrícula, turma e e-mail opcionais | Um aluno possui vários empréstimos |
| `Emprestimo` | Vincular aluno e livro; registrar prazo, devolução e cancelamento | Pertence a um aluno e um livro |
| `Usuario` | Operador que acessa a biblioteca, com perfil e situação ativa | Vinculado à identidade do Supabase por `SupabaseAuthId` |
| `RegistroAuditoria` | Trilha imutável das mutações, sem copiar dados pessoais do aluno | Identifica operador, ação, entidade e instante |

Aluno é quem retira o livro; usuário é quem opera o sistema. Um cadastro de aluno não concede acesso à administração. Autor e categoria começam como textos em `Livro`; podem virar entidades próprias quando a equipe precisar de cadastros e filtros mais elaborados.

O empréstimo possui data de retirada, data prevista, data efetiva de devolução, observação opcional e contador de renovações. `Ativo` significa que ainda não foi devolvido; `Devolvido` significa que a devolução foi registrada. `atrasado` é calculado para empréstimos ativos cujo prazo já passou. O calendário operacional considera São Paulo. Assim a tela mantém o filtro da referência e pode sinalizar atraso separadamente sem persistir um status que ficaria obsoleto com a passagem do tempo.

Excluir um empréstimo marca `CanceladoEm`: o registro sai das consultas comuns e um exemplar é liberado caso o empréstimo estivesse ativo. Isso permite remover um lançamento incorreto preservando seu registro no banco. A disponibilidade, a criação e as operações de devolução/cancelamento precisam continuar consistentes em acessos simultâneos.

Livro e aluno carregam uma `versao` opaca. Toda atualização exige a versão lida pelo cliente e troca esse valor de forma atômica; uma escrita baseada em dados antigos recebe `409` em vez de sobrescrever uma alteração mais recente. Mutações de livros, alunos e empréstimos gravam a auditoria na mesma transação da operação principal.

## Banco e autenticação

SQLite serve para a demonstração local; PostgreSQL no Supabase é a opção de banco compartilhado. O Entity Framework Core organiza o mapeamento e a evolução das tabelas. Cada provedor mantém suas próprias migrations, pois o SQL gerado depende do banco. [Migrations com múltiplos provedores no EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers).

No modo real, o front-end autentica no Supabase e envia o access token à API. A API valida o token e consulta um `Usuario` ativo com o mesmo identificador de autenticação. A policy `Operador` aceita `Administrador` e `Bibliotecario`; a policy `Administrador` aceita somente o primeiro. Quando as duas policies são exigidas pela mesma rota, o perfil confiável é consultado uma única vez por requisição. A autorização não depende de um perfil que o navegador possa escolher no corpo de uma requisição ou nos metadados do JWT.

O modo `Development` substitui esse fluxo por um administrador fictício para os testes locais. Ele exige opt-in explícito, ambiente `Development` e uma conexão originada de loopback; o servidor de testes em memória é reconhecido separadamente. A configuração é rejeitada em outros ambientes. Consulte [Supabase](SUPABASE.md) para migrar ao fluxo real.

Em PostgreSQL, migrations usam uma credencial administrativa temporária. A API publicada usa a role `biblioteca_runtime`, sem DDL e sem acesso ao histórico do Entity Framework. Essa separação reduz o impacto de um comprometimento da aplicação.

## Proteções HTTP

O host da requisição é validado contra `AllowedHosts`, que não aceita curingas globais. Os endpoints da API possuem rate limiting particionado pelo operador autenticado ou pelo endereço remoto, e o servidor rejeita corpos acima do limite configurado. Os endpoints de liveness e readiness não exigem token para que a plataforma de hospedagem possa monitorá-los; eles expõem apenas `ok` ou `unavailable`. A busca PostgreSQL usa `ILIKE` com padrões escapados e índices trigram, evitando que `%` e `_` recebidos do usuário mudem a semântica da pesquisa.

## Limites desta primeira entrega

A foto serve como referência visual e funcional parcial. Ainda faltam o contrato do front-end existente e requisitos confirmados pelo professor. A base não implementa multas, reservas, catálogo por exemplar físico ou administração de usuários pela API. Alterações nessas áreas devem ser combinadas com a equipe antes de modificar o contrato.
