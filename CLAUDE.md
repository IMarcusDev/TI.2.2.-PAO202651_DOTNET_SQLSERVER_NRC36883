# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

University assignment TI.2.2 (Arquitectura de Software, ESPE): **CountryAPI** in **.NET + SQL Server** using MVC. It is a file-by-file translation of the Java + MySQL version (TI.2.1, `../TI.2.1. PAO202651_JAVA_MYSQL_NRC36883`), which is finished and accepted. Same endpoints, JSON, messages, HTTP codes, validations, screens and flows; only the technology changes. A Java client must work against the .NET server and vice versa. All code, comments, messages and READMEs are in **Spanish**.

The full brief is [PROMPT_CLAUDE_CODE_TI22.md](PROMPT_CLAUDE_CODE_TI22.md). The plan and the Java → C# file map are in [PLAN.md](PLAN.md). Read both before implementing anything.

## Source of truth and hard rules

- **Behavior** (logic, messages, HTTP codes, validations, flows, screens, UI text): the TI.2.1 **code** and its `DUDAS.md` (decisions A1 to G7). TI.2.1 is read-only: never modify anything there.
- **Technology, .NET class and method names, database:** the TI.2.2 docs (`01. UML`, `02. MER`, `03. BDD`).
- When the two clash, Java wins on behavior and the TI.2.2 docs win on technology and names. Log every clash in `DUDAS.md` (section H onwards).
- **Never modify** `01. UML/`, `02. MER/`, `03. BDD/init.sql`, `03. BDD/seed.sql` or the taller `.docx`. Support files may be added to `03. BDD/` (for example `docker-compose.yml`).
- Do not add, remove or "improve" functionality. Any unavoidable behavior difference goes to `DUDAS.md` with options and a recommendation, and is asked before deciding.
- Work in phases: server → WPF → MAUI → console → web. Stop after each phase and report what was done, how to test it and what doubts remain.
- **Commits:** the author asked for one commit per finished, verified feature (not per file), on `main`. Do not delete files you did not create.

## Where things are documented

| Need | Source |
|---|---|
| Behavior to replicate | TI.2.1 code (`04. */*_JAVA_RESTFul`), its `DUDAS.md`, `PLAN.md` and READMEs |
| DB schema (`ApiPaisesDB`: `Usuarios`, `Regiones`, `Paises`, `Universidades`, PascalCase columns) and test data | `03. BDD/init.sql`, `03. BDD/seed.sql` (users `monster/monster9`, `admin/admin123`; SHA-256 hashes) |
| .NET class, method and property names | `01. UML/03. DIAGRAMAS/04_CLASES/DC_Servidor.oom`, `DC_Clientes.oom` |
| Requirements, use cases, sequence/activity diagrams, architecture | `01. UML/01. ERS`, `02. ECUD`, `03_SECUENCIA`, `02_ACTIVIDAD`, `05_ARQUITECTURA` |
| Decisions specific to .NET | `DUDAS.md` (H...) |

`.docx` files: convert with `pandoc`. `.oom`, `.pdm` and `.eam` files are PowerDesigner XML (`o:Class`, `c:Attributes`, `c:Operations`, `a:Name`). Write extracts **outside** the repository (scratchpad).

## Architecture

```
03. BDD/docker-compose.yml                        SQL Server 2022 + one-shot sqlcmd service (init.sql, seed.sql)
04. SERVIDOR/WS_COUNTRYAPI_DOTNET_RESTFul         ASP.NET Core Web API on Kestrel :5088 (0.0.0.0), base path /api
04. CLIESC/ESC_COUNTRYAPI_DOTNET_RESTFul          WPF, DataGrid
04. CLIMOV/MOV_COUNTRYAPI_DOTNET_RESTFul          .NET MAUI (Android); emulator → http://10.0.2.2:5088/api
04. CLICON/CONSOLE_COUNTRYAPI_DOTNET_RESTFul      .NET console app, numbered text menu
04. CLIWEB/WEB_COUNTRYAPI_DOTNET_RESTFul          ASP.NET Core MVC + Razor
```

Each project keeps its tests in `pruebas/` at the project root, next to the architecture folders, as a separate xUnit project excluded from the main `.csproj` (`<Compile Remove="pruebas\**" />`). The professor requires this layout.

**Server** (root namespace `ec.edu.monster`, flat folders): `ws` (`LoginWebService`, `PaisWebService`, ... as ASP.NET controllers with `[Route("api/Login")]`, ...) → `controladores` → `modelos/repositorios` (DAOs behind `modelos/interfaces`, receiving `AppDbContext`). `servicios`: `TokenService` (singleton, in-memory UUID, 15-minute sliding expiry), `HashService` (SHA-256), `ConsumoAPI` and `ConsumoAPIUniversidades` (`HttpClient`, 15 s timeout, `Newtonsoft.Json`). `config`: `AppDbContext` (EF Core, SQL Server). `Program.cs` wires DI.

Contracts that cut across the server (identical to Java):
- Every endpoint returns `RespuestaDTO<T>` `{estado: "OK"|"ERROR", mensaje, datos}`. On error `datos` is `null`. Messages never expose SQL or stack traces.
- JSON must be byte-compatible with the Java server (checked against the live Java server, see `DUDAS.md` H8): string enums, camelCase in Java's property order, `"datos":null` written but nulls inside `datos` omitted, doubles written like Java's `Double.toString` (`276841.0`, `1.7098242E7`), unescaped accents.
- The `Authorization` header is accepted with or without `Bearer `.
- Exceptions → HTTP in `RespuestaDTO.CodigoHttp`: `ArgumentException` 400, `UnauthorizedAccessException` 401, `KeyNotFoundException` 404, `InvalidOperationException` 409, `HttpRequestException`/`TaskCanceledException`/`IOException` 502, DB errors 500 «Servicio no disponible, intente más tarde», anything else 500 «Ocurrió un error inesperado, intente nuevamente». No custom exception classes.
- MySQL compared text without case **or accents**; SQL Server's default collation is accent-sensitive. Every text comparison Java delegates to MySQL (LIKE searches, region/user/country/university equality) uses `EF.Functions.Collate(..., "Latin1_General_CI_AI")`. LIKE escapes `\`, `%`, `_` and also `[`.
- Bulk imports load existing rows once, then save everything with one `SaveChanges` inside one transaction (rollback → 500). If the external API fails, respond 502 before touching the DB.

**Clients**: the four share the same `modelos`, `servicios` (`ClienteRest` with `HttpClient` + `Newtonsoft.Json`, `SesionServicio`, per-module services), `controlador`, `config/ConfigServidor` (`URL_BASE`, `TIMEOUT_SEGUNDOS = 15`, read from a config file) and `vista` interfaces (named as in the diagram, no `I` prefix). Translate them once and copy them; only the views differ. Clients talk only to the server. A 401 clears the session and returns to login.

## Commands

```bash
# Database (Docker Desktop). First start creates ApiPaisesDB and loads the seed.
cd "03. BDD" && docker compose up -d
docker compose down -v && docker compose up -d      # reset to the seed

# Server
cd "04. SERVIDOR/WS_COUNTRYAPI_DOTNET_RESTFul"
dotnet run                                           # http://0.0.0.0:5088/api
dotnet test pruebas                                  # unit tests
dotnet test pruebas --filter "FullyQualifiedName~TokenServiceTest"
bash pruebas/curl_countryapi.sh                      # adapted curl suite (exits with the number of failures)

# Equivalence check with the untouched Java suite (its 502 section needs asadmin, so skip it)
ASADMIN=/no/existe BASE=http://localhost:5088/api bash "../../../TI.2.1. PAO202651_JAVA_MYSQL_NRC36883/04. SERVIDOR/WS_COUNTRYAPI_JAVA_RESTFul/pruebas/curl_countryapi.sh"

# MAUI (from PowerShell, through a short junction: aapt2 fails on long paths)
dotnet build C:\climovcountry\MOV_COUNTRYAPI_DOTNET_RESTFul.csproj -f net10.0-android
```

Environment gotchas:

- **REST Countries is v5, not the documented v3.1** (TI.2.1 `DUDAS.md` C1): `https://api.restcountries.com/countries/v5`, `Authorization: Bearer <key>`, `data.objects`, 100 per page. The key comes from the env var `COUNTRYAPI_RESTCOUNTRIES_CLAVE` or `dotnet user-secrets set CountryApi:RestCountriesClave <key>`. Never commit it. Free plan: 1000 requests/month (an import uses 3, a name lookup 1): use the fake server in tests.
- **Pointing the external APIs elsewhere:** `CountryApi:RestCountriesUrl` and `CountryApi:UniversidadesUrl`, read on every request. The server also loads an optional, gitignored `appsettings.Pruebas.json` (reloaded on change); the adapted curl suite uses it for the 502 cases.
- **TI.2.1 for comparisons:** the Java server runs on Payara :8080 (`/WS_COUNTRYAPI_JAVA_RESTFul/api`) against the `countryapi-mysql` container. Compare real responses, not assumptions. Clean up any test data you create there.
- **Long paths:** MAUI's aapt2 breaks on paths over 260 characters; build from PowerShell through a junction in `C:\`.

## Reference projects (visual design and code layout only, not business logic)

- UI style: `..\..\PARCIAL_1\PAO202651_EXAMEN_CELULARES_RESTFUL_DOTNET_GR04\04. CLI*` (blue top bar, card menu, light-blue table headers, green/red alerts, server-status indicator). But the screens themselves (texts, cards, colors, alerts) are copied from the TI.2.1 Java views. Palette: azul `#1F4E79`, azul oscuro `#183D60`, azul claro `#E8EFF7`, fondo `#F3F4F6`, texto `#1F2933`, texto suave `#4B5563`, borde `#D1D5DB`, verde `#22C55E`, rojo `#B91C1C`, rojo claro `#FDECEC`.
- .NET structure (`.csproj`, WPF styles, Razor, MAUI pages, xUnit in `pruebas/`): `..\..\PARCIAL_1\CONBDD\TG.1.4. PAO202651_RESTFUL_DOTNET_CONBDD_NRC36883_GR04\04. *`.
- App title is **CountryAPI**. Do not carry over text, class names or data from EurekaBank or Celulares.
