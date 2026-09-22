# Requisições HTTP locais

O arquivo `BibliotecaEscolar.http` lê `baseUrl` e `accessToken` do ambiente escolhido pelo cliente HTTP.

Selecione o ambiente `development` de `http-client.env.json`, execute primeiro o login do arquivo `.http` e copie o token retornado para uma cópia privada do ambiente.

Para testar sem versionar a sessão:

1. copie `http-client.private.env.example.json` para `http-client.private.env.json`;
2. preencha a URL da API e o access token apenas na cópia privada;
3. selecione esse ambiente ao executar as demais requisições.

`http-client.private.env.json` é ignorado pelo Git. Não coloque credenciais no `.http`, no arquivo de exemplo nem em `http-client.env.json`. O access token ainda fica em texto simples na máquina; apague o arquivo quando terminar e use um token de curta duração.
