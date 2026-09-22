# Arquitetura e decisões iniciais

## Escopo observado e hipóteses

A aplicação possui os menus Início, Livros e Empréstimos. Na retirada, o bibliotecário digita o nome do aluno; não há cadastro de alunos. A tela de empréstimos oferece pesquisa por aluno ou livro, filtros, criação, devolução e exclusão.

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
| `Emprestimo` | Guardar o nome digitado na retirada e vincular o livro; registrar prazo, devolução e cancelamento | Pertence a um livro |
| `Usuario` | Único bibliotecário que acessa o sistema, com login e hash da senha | Possui sessões revogáveis em `SessaoUsuario` |
| `SessaoUsuario` | Hash de um token aleatório, expiração e eventual revogação | Pertence ao bibliotecário |
| `RegistroAuditoria` | Trilha imutável das mutações, sem copiar o nome do aluno | Identifica operador, ação, entidade e instante |

O nome de quem retira o livro é armazenado no próprio empréstimo. `Usuario` é somente o operador autenticado que usa o sistema. Autor e categoria começam como textos em `Livro`; podem virar entidades próprias quando a equipe precisar de cadastros e filtros mais elaborados.

O empréstimo possui data de retirada, data prevista, data efetiva de devolução, observação opcional e contador de renovações. `Ativo` significa que ainda não foi devolvido; `Devolvido` significa que a devolução foi registrada. `atrasado` é calculado para empréstimos ativos cujo prazo já passou. O calendário operacional considera São Paulo. Assim a tela mantém o filtro da referência e pode sinalizar atraso separadamente sem persistir um status que ficaria obsoleto com a passagem do tempo.

Excluir um empréstimo marca `CanceladoEm`: o registro sai das consultas comuns e um exemplar é liberado caso o empréstimo estivesse ativo. Isso permite remover um lançamento incorreto preservando seu registro no banco. A disponibilidade, a criação e as operações de devolução/cancelamento precisam continuar consistentes em acessos simultâneos.

Livro carrega uma `versao` opaca. Toda atualização exige a versão lida pelo cliente e troca esse valor de forma atômica; uma escrita baseada em dados antigos recebe `409` em vez de sobrescrever uma alteração mais recente. Mutações de livros e empréstimos gravam a auditoria na mesma transação da operação principal.

## Banco e autenticação

SQLite serve para a demonstração local; PostgreSQL no Supabase é a opção de banco compartilhado. O Entity Framework Core organiza o mapeamento e a evolução das tabelas. Cada provedor mantém suas próprias migrations, pois o SQL gerado depende do banco. [Migrations com múltiplos provedores no EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers).

No modo real, o front-end envia login e senha a `POST /api/v1/auth/login`. A API compara a senha com o hash PBKDF2 salvo em `Usuarios`, cria um token aleatório de 256 bits e persiste somente o SHA-256 desse token. As rotas de negócio exigem a policy `Operador`, que aceita apenas o bibliotecário ativo. O logout revoga a sessão e as sessões expiram no servidor.

O modo `Development` substitui esse fluxo por um bibliotecário fictício exclusivamente nos testes automatizados. Ele exige opt-in explícito e ambiente `Development`; a configuração é rejeitada em outros ambientes. A execução normal, inclusive local, usa `Auth:Mode=Database`.

Em PostgreSQL, migrations usam uma credencial administrativa temporária. A API publicada usa a role `biblioteca_runtime`, sem DDL e sem acesso ao histórico do Entity Framework. Essa separação reduz o impacto de um comprometimento da aplicação.

## Proteções HTTP

O host da requisição é validado contra `AllowedHosts`, que não aceita curingas globais. Os endpoints da API possuem rate limiting particionado pelo operador autenticado ou pelo endereço remoto, e o servidor rejeita corpos acima do limite configurado. Os endpoints de liveness e readiness não exigem token para que a plataforma de hospedagem possa monitorá-los; eles expõem apenas `ok` ou `unavailable`. A busca PostgreSQL usa `ILIKE` com padrões escapados e índices trigram, evitando que `%` e `_` recebidos do usuário mudem a semântica da pesquisa.

## Limites desta primeira entrega

A foto serve como referência visual e funcional parcial. Ainda faltam o contrato do front-end existente e requisitos confirmados pelo professor. A base não implementa multas, reservas, catálogo por exemplar físico ou administração de usuários pela API. Alterações nessas áreas devem ser combinadas com a equipe antes de modificar o contrato.
