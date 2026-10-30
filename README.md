# Authentication API

API de autenticação para fins de estudo, construída em **.NET 10** seguindo
Clean Architecture, atuando como **Identity Provider OAuth 2.0 / OpenID
Connect** (Authorization Code + PKCE) com cadastro/login local de usuários.

> Projeto de estudo. Consulte [CLAUDE.md](CLAUDE.md) para as diretrizes
> arquiteturais e de segurança seguidas no desenvolvimento.

## Stack

- .NET 10 / C# 14 — ASP.NET Core (Minimal APIs)
- Entity Framework Core 10 + **PostgreSQL** (Npgsql)
- **OpenIddict** (servidor OAuth2/OIDC) + **ASP.NET Core Identity** (usuários locais)
- MediatR (CQRS) + FluentValidation
- Swagger / OpenAPI (Swashbuckle) — apenas em Development
- xUnit + FluentAssertions + NSubstitute

## Estrutura

```text
src/
├── Core/
│   ├── Domain/           # Entidades, Result pattern, erros de domínio
│   └── Application/      # Commands/Queries (MediatR), DTOs, validações
├── Infrastructure/       # DbContext, Identity, configuração do OpenIddict
└── API/                   # Minimal APIs, Program.cs, Swagger, login
tests/
└── Application.UnitTests/
postman/                  # Collection + Environment para testes manuais
```

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (para PostgreSQL local e/ou execução containerizada)
- Ferramenta `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## Configuração local (sem Docker Compose)

1. **Banco de dados** — suba um PostgreSQL local, por exemplo:

   ```bash
   docker run -d --name authentication-dev-postgres \
     -e "POSTGRES_USER=authentication" \
     -e "POSTGRES_PASSWORD=<sua-senha-local>" \
     -e "POSTGRES_DB=AuthenticationDb" \
     -p 5433:5432 \
     postgres:18
   ```

2. **Connection string via User Secrets** (nunca commitar segredos):

   ```bash
   cd src/API
   dotnet user-secrets set "ConnectionStrings:AppDb" \
     "Host=localhost;Port=5433;Database=AuthenticationDb;Username=authentication;Password=<sua-senha-local>"
   ```

3. **Certificado HTTPS de desenvolvimento** (se ainda não confiou em um):

   ```bash
   dotnet dev-certs https --trust
   ```

4. **Executar a API**:

   ```bash
   dotnet run --project src/API --launch-profile https
   ```

   Em ambiente `Development`, ao iniciar a aplicação:
   - as migrations são aplicadas automaticamente;
   - um client OAuth de exemplo (`spa-client`) é registrado, com PKCE
     obrigatório e redirect URIs para teste local e para o Postman
     (`https://oauth.pstmn.io/v1/callback`).

5. **Swagger**: abra `https://localhost:7179/swagger` no navegador.

## Testando a API

### Swagger (navegador)

1. `POST /api/accounts/register` → crie um usuário de teste.
2. Clique em **Authorize** (OAuth2 configurado com `spa-client` + PKCE).
3. Você será redirecionado para `/account/login`; autentique com o usuário criado.
4. O Swagger recebe o token automaticamente; teste `GET /api/me`.

### Postman

Importe [postman/Authentication.postman_collection.json](postman/Authentication.postman_collection.json)
e [postman/Authentication.postman_environment.json](postman/Authentication.postman_environment.json).
Instruções de uso estão na descrição da collection (aba Authorization de
cada requisição).

## Executando via Docker

```bash
cp .env.example .env   # preencha as variáveis com valores locais
docker compose up --build
```

O `docker-compose.yml` sobe a API (`http://localhost:8080`,
`https://localhost:8443`) e um container PostgreSQL. Nenhum segredo fica no
repositório — tudo vem do `.env` (git-ignorado) e de certificados locais
montados em `./certs` (também git-ignorado).

## Testes automatizados

```bash
dotnet build Authentication.slnx
dotnet test Authentication.slnx
```

## Segurança (resumo)

- Apenas **Authorization Code + PKCE** e **Refresh Token**; nenhum fluxo
  Implicit ou Resource Owner Password Credentials.
- Senhas com hash via ASP.NET Core Identity (nunca texto puro).
- Segredos (connection strings, certificados, client secrets) apenas via
  User Secrets (dev) ou variáveis de ambiente/volumes (produção) — nunca
  no código-fonte.
- `TreatWarningsAsErrors` habilitado em toda a solução.

Mais detalhes e regras de contribuição em [CLAUDE.md](CLAUDE.md).
