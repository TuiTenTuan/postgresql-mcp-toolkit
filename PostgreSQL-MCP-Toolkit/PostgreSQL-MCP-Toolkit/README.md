# PostgreSQL MCP Toolkit

Production-ready ASP.NET Core MCP server that exposes PostgreSQL inspection and SQL execution tools over the Model Context Protocol (HTTP, stateless). Designed for isolated Docker deployment.

## Requirements

- .NET 10 SDK (local development)
- PostgreSQL database reachable from the server
- Docker (for container deployment)

## Environment variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `POSTGRES_HOST` | Yes | — | PostgreSQL hostname or IP |
| `POSTGRES_PORT` | No | `5432` | PostgreSQL port |
| `POSTGRES_USER` | Yes | — | Database user |
| `POSTGRES_PASSWORD` | Yes | — | Database password |
| `POSTGRES_DATABASE` | Yes | — | Database name |
| `API_KEY` | Yes | — | Shared secret sent by clients in the `x-api-key` header |
| `ASPNETCORE_URLS` | No | `http://+:8080` in the image | Bind URL(s) for Kestrel |

The server fails fast at startup if any required variable is missing or empty.

## Authentication

- Protected routes (including MCP endpoints) require header: `x-api-key: <API_KEY>`
- Missing or invalid keys return `401 Unauthorized`
- `/health` is unauthenticated for container health checks

## MCP tools

| Tool | Description |
|---|---|
| `list_tables` | Lists base tables as schema-qualified names (`schema.table`) |
| `get_table_structure` | Returns columns, types, nullability, defaults, and primary-key flags |
| `execute_sql` | Executes caller-supplied SQL (read/write); 30s command timeout |

## Local run

```powershell
$env:POSTGRES_HOST = "localhost"
$env:POSTGRES_PORT = "5432"
$env:POSTGRES_USER = "postgres"
$env:POSTGRES_PASSWORD = "CHANGE_ME"
$env:POSTGRES_DATABASE = "mydb"
$env:API_KEY = "CHANGE_ME_API_KEY"
$env:ASPNETCORE_URLS = "http://localhost:8080"

dotnet run --project PostgreSQL-MCP-Toolkit.csproj
```

Health check:

```text
GET http://localhost:8080/health
```

MCP client example (VS Code / Cursor):

```json
{
  "servers": {
    "PostgreSQL-MCP-Toolkit": {
      "type": "http",
      "url": "http://localhost:8080",
      "headers": {
        "x-api-key": "CHANGE_ME_API_KEY"
      }
    }
  }
}
```

## Docker

Build (from this directory):

```bash
docker build -t postgresql-mcp-toolkit .
```

Run:

```bash
docker run --rm -p 8080:8080 \
  -e POSTGRES_HOST=postgres.example.com \
  -e POSTGRES_PORT=5432 \
  -e POSTGRES_USER=app_user \
  -e POSTGRES_PASSWORD=CHANGE_ME \
  -e POSTGRES_DATABASE=app_db \
  -e API_KEY=CHANGE_ME_API_KEY \
  -e ASPNETCORE_URLS=http://+:8080 \
  postgresql-mcp-toolkit
```

### docker compose (example)

```yaml
services:
  postgresql-mcp-toolkit:
    build: .
    ports:
      - "8080:8080"
    environment:
      POSTGRES_HOST: postgres
      POSTGRES_PORT: "5432"
      POSTGRES_USER: app_user
      POSTGRES_PASSWORD: CHANGE_ME
      POSTGRES_DATABASE: app_db
      API_KEY: CHANGE_ME_API_KEY
      ASPNETCORE_URLS: http://+:8080
```

Replace all `CHANGE_ME*` placeholders before use. Do not bake secrets into the image.

## More information

- [Model Context Protocol](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://csharp.sdk.modelcontextprotocol.io/)
- [ModelContextProtocol.AspNetCore](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore)
