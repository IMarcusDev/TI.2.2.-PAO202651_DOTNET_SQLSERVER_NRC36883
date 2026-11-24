# Prompt para Claude Code: migrar CountryAPI de Java a .NET + SQL Server (TI.2.2)

---

## Tu tarea

El sistema **CountryAPI** ya está implementado, probado y aceptado en Java + MySQL (TI.2.1):

```
C:\IMarcusDev\Universidad\8vo_semestre\ArquitecturaSw_36883_GR04\PARCIAL_2\TI.2.1. PAO202651_JAVA_MYSQL_NRC36883
```

Tu único trabajo es **replicar ese código en .NET con SQL Server**, en esta carpeta (TI.2.2).

- **No es un diseño nuevo.** Es una traducción, archivo por archivo, que conserva exactamente el mismo comportamiento. Solo cambia la tecnología.
- **No agregues ni quites funcionalidad.** Tampoco «mejores» nada.
- **El resultado esperado:** los mismos endpoints, el mismo JSON, los mismos mensajes, los mismos códigos HTTP, las mismas validaciones, las mismas pantallas y los mismos flujos. Un cliente Java debería poder usar el servidor .NET sin notar la diferencia, y al revés.
- **No toques TI.2.1.** Ahí solo lees.

### Qué manda en cada cosa

| Tema                                                                                                   | Fuente de verdad                                                                                              |
| ------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------- |
| Comportamiento: lógica, mensajes, códigos HTTP, validaciones, flujos, pantallas, textos de la interfaz | **El código de TI.2.1** y su `DUDAS.md` (decisiones A1 a G7, ya aceptadas por el autor)                       |
| Contexto y comandos de TI.2.1                                                                          | `TI.2.1/CLAUDE.md` y `TI.2.1/PROMPT_CLAUDE_CODE_TI21.md`                                                      |
| Tecnología, nombres de clases y métodos en .NET, base de datos                                         | La documentación de **TI.2.2**: `01. UML` (ERS, ECUD, diagramas), `02. MER` y `03. BDD/init.sql` y `seed.sql` |

**Si las dos fuentes chocan,** el código Java manda en el comportamiento y la documentación de TI.2.2, en la tecnología y los nombres.

Ejemplo: la documentación dice REST Countries v3.1, pero esa versión fue retirada y el código Java usa la v5 con clave (TI.2.1/`DUDAS.md`, C1). Tú usas la v5.

Anota cada choque que encuentres en `DUDAS.md` (en TI.2.2).

### Reglas

- No modifiques `01. UML/`, `02. MER/`, `03. BDD/init.sql`, `03. BDD/seed.sql` ni el `.docx` del taller.
    - Puedes **agregar** archivos de apoyo en `03. BDD/`, como un `docker-compose.yml`, igual que en TI.2.1.
- No hagas commits ni borres archivos que no hayas creado tú.
- Cualquier ambigüedad o diferencia de comportamiento que no puedas evitar entre Java y .NET:
    - se anota en `DUDAS.md` con las opciones y tu recomendación;
    - **se pregunta antes de decidir**.
- Todo el código, los comentarios, los mensajes y los README van en **español**.

## Fase 0: leer y planificar (antes de escribir código)

1. **Lee en TI.2.1:**
    - `CLAUDE.md`, `PLAN.md` y `DUDAS.md` completos.
    - El código de los cinco proyectos:
        - `04. SERVIDOR/WS_COUNTRYAPI_JAVA_RESTFul`;
        - `04. CLIESC/ESC_COUNTRYAPI_JAVA_RESTFul`;
        - `04. CLIMOV/MOV_COUNTRYAPI_JAVA_RESTFul`;
        - `04. CLICON/CONSOLE_COUNTRYAPI_JAVA_RESTFul`;
        - `04. CLIWEB/WEB_COUNTRYAPI_JAVA_RESTFul`.
    - Sus `README.md`.
    - La suite `04. SERVIDOR/WS_COUNTRYAPI_JAVA_RESTFul/pruebas/curl_countryapi.sh`.
2. **Lee en TI.2.2:**
    - `03. BDD/init.sql` y `seed.sql`: base `ApiPaisesDB`, tablas `Usuarios`, `Regiones`, `Paises` y `Universidades`, columnas en PascalCase.
    - `02. MER/02_MER_COUNTRYAPI.pdm`.
    - El ERS y el ECUD `.docx`.
    - `01. UML/03. DIAGRAMAS/04_CLASES/DC_Servidor.oom` y `DC_Clientes.oom`: nombres .NET en PascalCase, `AppDbContext`, `Program.cs`.
    - Los DS, los DA y la ARQ.

    Los `.docx` se convierten con `pandoc`, `python-docx` o descomprimiendo `word/document.xml`. Los `.oom`, `.pdm` y `.eam` son XML de PowerDesigner (`o:Class`, `c:Attributes`, `c:Operations`, `a:Name`). Deja los extractos **fuera** del repositorio.

3. **Revisa el entorno:**
    - `dotnet --list-sdks` y `dotnet --list-runtimes`;
    - si hay SQL Server local (`sqlcmd -L`, servicios `MSSQL*`, LocalDB) o Docker Desktop;
    - si están las cargas de trabajo de MAUI/Android (`dotnet workload list`).
4. **Escribe `PLAN.md`** con:
    - la tabla de correspondencia **archivo Java → archivo C#** de cada proyecto;
    - los nombres finales de los proyectos .NET;
    - el plan por fases;
    - las dudas.
5. **Crea un `CLAUDE.md` para TI.2.2**, con la misma estructura que el de TI.2.1 adaptada a .NET.
6. **Detente y espera mi confirmación.**

## Correspondencia de tecnología

### Servidor

| Java (TI.2.1)                                                                                                | .NET (TI.2.2)                                                                                                                                                                                                                                                                                                          |
| ------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `WS_COUNTRYAPI_JAVA_RESTFul`, Maven `war` en Payara :8080, contexto `/WS_COUNTRYAPI_JAVA_RESTFul/api`        | `WS_COUNTRYAPI_DOTNET_RESTFul`, ASP.NET Core Web API en Kestrel :5088, ruta base `/api`                                                                                                                                                                                                                                |
| Recursos JAX-RS en `ws` (`LoginWebService`, `PaisWebService`, ...)                                           | Controladores de ASP.NET con **los mismos nombres de clase** del `DC_Servidor` .NET (`LoginWebService`, ...) y las mismas rutas (`/api/Login/login`, `/api/Paises/buscar`, ...)                                                                                                                                        |
| `ApplicationConfig`                                                                                          | `Program.cs` (inyección de dependencias), como en el diagrama                                                                                                                                                                                                                                                          |
| `DBConfig` + JDBC + `PreparedStatement`                                                                      | `AppDbContext : DbContext` con EF Core y el proveedor de SQL Server. Los DAO reciben `AppDbContext`, como en el `DC_Servidor` .NET                                                                                                                                                                                     |
| Importación con `executeBatch` en **una** transacción                                                        | Una transacción de EF Core (`BeginTransaction` y un `SaveChanges`). Se cargan los existentes una sola vez, igual que en Java                                                                                                                                                                                           |
| `db.properties`                                                                                              | `ConnectionStrings` en `appsettings.json`, sin claves reales en el repositorio; `appsettings.Development.json` o variables de entorno para las reales                                                                                                                                                                  |
| `api.properties` (clave de REST Countries, fuera de git)                                                     | `dotnet user-secrets` o la **misma** variable de entorno `COUNTRYAPI_RESTCOUNTRIES_CLAVE`. **Nunca** escribas la clave en el código ni en archivos versionados                                                                                                                                                         |
| Propiedades del sistema `countryapi.restcountries.url` y `countryapi.universidades.url` (para la prueba 502) | Equivalentes en la configuración (`appsettings` o variables de entorno), leídos en cada petición                                                                                                                                                                                                                       |
| JSON-B para el contrato y Gson para las APIs externas                                                        | `System.Text.Json` para el contrato (camelCase) y `Newtonsoft.Json` para las APIs externas, como dice el ERS .NET                                                                                                                                                                                                      |
| `HttpURLConnection` con 15 s                                                                                 | `HttpClient` (`IHttpClientFactory`) con `Timeout` de 15 s                                                                                                                                                                                                                                                              |
| Excepciones estándar → código HTTP (`RespuestaDTO.codigoHttp`)                                               | El mismo mapeo con sus equivalentes .NET: `ArgumentException` → 400, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404, `InvalidOperationException` → 409, `HttpRequestException` / `TaskCanceledException` / `IOException` → 502, el resto → 500 con el mensaje fijo. Sin clases de excepción propias |

### Clientes

| Cliente    | Java (TI.2.1)                         | .NET (TI.2.2)                                                                                 |
| ---------- | ------------------------------------- | --------------------------------------------------------------------------------------------- |
| Escritorio | Swing con `JTable`                    | **WPF** con `DataGrid` (`ESC_COUNTRYAPI_DOTNET_RESTFul`)                                      |
| Móvil      | Android Java                          | **.NET MAUI** para Android (`MOV_COUNTRYAPI_DOTNET_RESTFul`). En el emulador, `10.0.2.2:5088` |
| Consola    | Maven, menú de texto                  | **Aplicación de consola .NET** (`CONSOLE_COUNTRYAPI_DOTNET_RESTFul`)                          |
| Web        | JSP + Servlets en Payara              | **ASP.NET Core MVC** con Razor (`WEB_COUNTRYAPI_DOTNET_RESTFul`)                              |
| JSON       | Gson                                  | `Newtonsoft.Json`                                                                             |
| HTTP       | `ClienteRest` con `HttpURLConnection` | `ClienteRest` con `HttpClient`                                                                |
| Pruebas    | JUnit 5 con servidor falso            | xUnit (o el esquema de pruebas de las referencias .NET) con un servidor falso equivalente     |

Reglas de la traducción de los clientes:

- Las capas `modelos`, `servicios`, `controlador`, `config` y las **interfaces de vista** son iguales en los cuatro clientes, como en Java (DUDAS D1, E1, F1 y G1). Tradúcelas **una vez**, con cuidado, y cópialas a los cuatro proyectos; solo cambian las vistas.
- Respeta las decisiones de Java sobre controladores y vistas: D2 a D4, E2 a E6, F2 a F7 y G2 a G7.
- En la web:
    - `CsrfFilter` y `AuthFilter` pasan a antiforgery y a un filtro o middleware de autenticación;
    - se mantienen POST/Redirect/GET, la sesión de 15 minutos y el cambio de id de sesión al iniciar sesión.

### Detalles que rompen la equivalencia si no los cuidas

1. **Formato del JSON.**
    - Los enums (`estado`) deben salir como texto (`"OK"` / `"ERROR"`), no como número: usa `JsonStringEnumConverter`.
    - Los nombres de las propiedades, el `null` frente al vacío, los números y las fechas deben ser idénticos a los del servidor Java. Compáralos con las respuestas reales del servidor Java, no con lo que supones.
2. **Búsquedas.**
    - En MySQL, la intercalación `utf8mb4_0900_ai_ci` hace que `LIKE` ignore mayúsculas **y tildes** («peru» encuentra «Perú»).
    - En SQL Server, la intercalación por defecto distingue tildes.
    - Usa `EF.Functions.Collate(..., "Latin1_General_CI_AI")` con `EF.Functions.Like` en las búsquedas, sin cambiar `init.sql`.
    - Escapa `%` y `_` como lo hace Java.
3. **Unicidad.** Por la misma razón, la restricción UNIQUE de MySQL trata «Perú» y «Peru» como el mismo nombre; la de SQL Server no. Revisa cómo Java detecta el país repetido (409) y cómo compara los nombres al hacer el upsert. Replícalo. Si no se puede igualar sin cambiar la base, anótalo en `DUDAS.md`.
4. **Orden.** `ORDER BY nombre` debe dar el mismo orden que en Java; las tildes ordenan distinto según la intercalación. Verifícalo con los datos del seed.
5. **Encabezado `Authorization`.** Se acepta con o sin `Bearer `.
6. **Token.** UUID con 15 minutos de expiración deslizante, en memoria. `TokenService` va registrado como singleton.
7. **REST Countries v5.**
    - Paginación con `limit=100&offset=N` y el mismo `response_fields`.
    - Mismo mapeo: `names.translations.spa.common`, la capital `primary`, `area.kilometers`, `currencies[]`.
    - Mismos valores por defecto: «Sin capital», «Sin subregión», «Sin moneda» y área 0.
    - Ve TI.2.1/`DUDAS.md` C1. Cuida la cuota de 1000 peticiones al mes: prueba con el servidor falso.

## Diseño visual

Las pantallas .NET deben **verse y comportarse como las de TI.2.1**: mismos textos, mismas tarjetas del menú, mismos colores, mismas alertas y mismo indicador del servidor. Toma el diseño de las vistas Java ya hechas, no lo vuelvas a inventar.

Para la forma de hacerlo en cada tecnología .NET (estilos WPF, Razor y CSS, páginas MAUI, estructura de los `.csproj`, pruebas), usa como referencia los proyectos .NET del grupo:

- `C:\IMarcusDev\Universidad\8vo_semestre\ArquitecturaSw_36883_GR04\PARCIAL_1\CONBDD\TG.1.4. PAO202651_RESTFUL_DOTNET_CONBDD_NRC36883_GR04\04. *`
    - Servidor, consola, WPF, MAUI y web .NET con BDD.
- `C:\IMarcusDev\Universidad\8vo_semestre\ArquitecturaSw_36883_GR04\PARCIAL_1\PAO202651_EXAMEN_CELULARES_RESTFUL_DOTNET_GR04\04. CLI*`
    - Es el diseño visual base: barra azul, menú de tarjetas, tablas con encabezado celeste y alertas.
    - Lee sus `README.md`.

No copies textos, clases ni datos de EurekaBank ni de Celulares.

## Versión de .NET

La documentación de TI.2.2 dice **.NET 8**. Las referencias del grupo usan **net10.0**.

- Si el runtime de .NET 8 está instalado, usa net8.0 como dice la documentación.
- Si no está, **pregúntame** antes de elegir, y anótalo en `DUDAS.md`.

## Base de datos

- Usa la instancia de SQL Server que ya tenga el equipo. Si no hay, crea `03. BDD/docker-compose.yml` con SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`).
- El contenedor no ejecuta scripts al arrancar, así que agrega un servicio o un script que corra `init.sql` y luego `seed.sql` con `sqlcmd`.
- Documenta el arranque y el reinicio como en TI.2.1.
- Comprueba que las tildes del seed queden bien («América», no «AmÃ©rica»).

## Forma de trabajo y verificación

- **Fases, deteniéndote al final de cada una** para decirme qué hiciste, cómo probarlo y qué dudas quedan:
    1. servidor;
    2. escritorio WPF;
    3. móvil MAUI;
    4. consola;
    5. web MVC.
- **Prueba de equivalencia del servidor (obligatoria).** Ejecuta la misma suite de Java contra el servidor .NET:

    ```bash
    BASE=http://localhost:5088/api bash "../TI.2.1. PAO202651_JAVA_MYSQL_NRC36883/04. SERVIDOR/WS_COUNTRYAPI_JAVA_RESTFul/pruebas/curl_countryapi.sh"
    ```

    - Todos los casos deben pasar con los mismos códigos y los mismos mensajes.
    - Si la prueba 502 depende de `asadmin`, crea en `04. SERVIDOR/WS_COUNTRYAPI_DOTNET_RESTFul/pruebas/` una copia adaptada que cambie la URL de la API con la configuración de .NET.
    - **No modifiques el script de TI.2.1.**

- **Prueba cruzada (recomendada).** Ejecuta el cliente de consola Java contra el servidor .NET y el de consola .NET contra el servidor Java. Si los dos funcionan igual, el contrato es el mismo.
- **Pruebas unitarias.** Traduce las pruebas de cada proyecto Java a .NET: las mismas pruebas, con los mismos casos. Indica cuántas hay y que todas pasan.
- **Compilación limpia.** `dotnet build` sin errores en todos los proyectos, y en MAUI, `dotnet build -f net*-android`.
- **README por proyecto** con la configuración (cadena de conexión, URL del servidor, clave de la API), cómo ejecutarlo y cómo correr las pruebas.
- **Bitácora de decisiones.** En `DUDAS.md` registra, por fase, solo lo que tuvo que cambiar respecto de Java por la tecnología, con el mismo formato que el de TI.2.1.
