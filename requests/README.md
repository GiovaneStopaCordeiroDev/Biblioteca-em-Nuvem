# Requisições HTTP locais

O arquivo `BibliotecaEscolar.http` lê `baseUrl` e `accessToken` do ambiente escolhido pelo cliente HTTP.

Para a demonstração, selecione o ambiente `development` de `http-client.env.json`. O valor de token desse ambiente é deliberadamente inerte; a autenticação demonstrativa só funciona em `Development`, quando habilitada explicitamente, e para uma conexão local.

Para testar a autenticação real:

1. copie `http-client.private.env.example.json` para `http-client.private.env.json`;
2. preencha a URL da API e o access token apenas na cópia privada;
3. selecione o ambiente `supabase` ao executar a requisição.

`http-client.private.env.json` é ignorado pelo Git. Não coloque credenciais no `.http`, no arquivo de exemplo nem em `http-client.env.json`. O access token ainda fica em texto simples na máquina; apague o arquivo quando terminar e use um token de curta duração.
