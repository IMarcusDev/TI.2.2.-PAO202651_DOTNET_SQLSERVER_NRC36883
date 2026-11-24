# Dudas y decisiones de la migración a .NET (CountryAPI · TI.2.2)

Las decisiones A1 a G7 de TI.2.1 (`../TI.2.1. PAO202651_JAVA_MYSQL_NRC36883/DUDAS.md`) siguen vigentes: el comportamiento es el del código Java. Aquí solo se registra lo que cambia por la tecnología, con el mismo formato: dónde está el choque, las opciones y la recomendación.

## H. Fase 0 (lectura y plan)

### H1. Versión de .NET — **necesito tu decisión**

- **Documentación de TI.2.2:** .NET 8 y EF Core 8 en todo (ERS, ARQ).
- **Este equipo:**
  - SDK solo 10.0.401;
  - de .NET 8 solo está el runtime base (`Microsoft.NETCore.App` 8.0.21), **sin** `AspNetCore.App` 8 (servidor y web) ni `WindowsDesktop.App` 8 (WPF);
  - las cargas de MAUI/Android son de .NET 10, y MAUI para .NET 8 ya no tiene soporte.
- **Referencias del grupo (TG.1.4, EXAMEN):** net10.0.

**Opciones:**

- **a) net10.0 en los cinco proyectos, con EF Core 10 (recomendada).** Compila y corre con lo instalado, igual que las referencias. Se documenta como diferencia con el ERS.
- **b) net8.0 en servidor, consola, WPF y web (EF Core 8), y net10.0 en MAUI.** Hay que instalar los runtimes ASP.NET Core 8 y Windows Desktop 8. MAUI no puede ir en .NET 8 de todos modos.
- **c) net8.0 en todo.** No es posible para MAUI con las cargas instaladas.

### H2. REST Countries v3.1 (documentación) frente a v5 (código Java)

Choque de fuentes. Manda Java (TI.2.1, C1): `https://api.restcountries.com/countries/v5`, clave en `Authorization: Bearer`, paginación `limit=100&offset=N`, el mismo `response_fields` y el mismo mapeo. La clave sale de la variable `COUNTRYAPI_RESTCOUNTRIES_CLAVE` o de `dotnet user-secrets` (`CountryApi:RestCountriesClave`); nunca de un archivo versionado. Las pruebas usan un servidor falso para no gastar la cuota de 1000 peticiones al mes.

### H3. Comparaciones de texto: MySQL no distingue tildes y SQL Server sí

En Java, MySQL (`utf8mb4_0900_ai_ci`) resuelve sin distinguir mayúsculas ni tildes:

- las búsquedas `LIKE` («peru» encuentra «Perú»);
- las igualdades: región («america» es América), usuario del login, `existePorNombre`, el upsert de «Guardar», y las universidades por país («ecuador» lista las de «Ecuador»).

En SQL Server la intercalación por defecto (`SQL_Latin1_General_CP1_CI_AS`) distingue tildes. **Decisión:** todas esas comparaciones usan `EF.Functions.Collate(columna, "Latin1_General_CI_AI")`, sin tocar `init.sql`. Las claves en memoria del upsert (importación de países y de universidades) se normalizan igual que en Java (sin tildes y en minúsculas).

### H4. Nombre repetido al **actualizar** un país

- **Java:** al registrar consulta `existePorNombre` (409). Al actualizar confía en el índice UNIQUE de MySQL, que trata «Peru» y «Perú» como el mismo nombre.
- **SQL Server:** el UNIQUE usa la intercalación de la base (`CI_AS`), así que renombrar un país a «Peru» cuando existe «Perú» **no** fallaría.
- **Decisión:** para dar el mismo 409 sin cambiar la base, `PaisDAO.Actualizar` comprueba antes si otro país (otro `Id`) tiene ese nombre con `Latin1_General_CI_AI`. Si aun así el UNIQUE salta (otro usuario guardó al mismo tiempo), la violación de clave (2601/2627) se convierte en 409, como el `SQLIntegrityConstraintViolationException` de Java.

### H5. Comodines de `LIKE`

Java escapa `\`, `%` y `_`. En SQL Server `[` también es comodín (`[a-z]`), y en MySQL no. **Decisión:** escapar además `[` para que «[» se busque literalmente, igual que en Java. Se usa `\` como carácter de escape.

### H6. Orden por nombre

`ORDER BY nombre` en MySQL (`0900_ai_ci`) y en SQL Server puede diferir con guiones, apóstrofos o letras con tilde. **Plan de verificación:** comparar el orden de los 254 nombres que ya tiene la MySQL de TI.2.1 (sin gastar cuota) con el `ORDER BY Nombre` de SQL Server, además del seed. Si hay diferencias, te las muestro con opciones antes de decidir.

### H7. Promedio poblacional

En SQL Server, `AVG` sobre `BIGINT` devuelve un entero truncado, y en MySQL un decimal que luego se redondea (`ROUND`). **Decisión:** el DAO obtiene la suma y la cantidad por región y calcula el promedio con `decimal`, redondeando como MySQL (a 4 decimales y luego al entero, alejándose de cero). 0 para las regiones sin países.

### H8. Formato del JSON del contrato

Comparado contra el servidor Java desplegado el 24/11/2026 (JSON-B de Payara):

| Detalle | Java | Cómo se iguala en .NET |
| --- | --- | --- |
| Enum `estado` | `"OK"` | `JsonStringEnumConverter` |
| Nombres | camelCase, en el orden de `@JsonbPropertyOrder` | Propiedades declaradas en ese orden |
| `datos` nulo | `"datos":null` se escribe | `[JsonIgnore(Condition = Never)]` en `RespuestaDTO` |
| Nulos dentro de `datos` | Se omiten (país sin `capital`) | `DefaultIgnoreCondition = WhenWritingNull` |
| `double` | `Double.toString`: `276841.0`, `0.5`, `1.7098242E7` | Convertidor propio que escribe el mismo texto |
| Fechas | `"2026-11-23T19:49:26"` | `DateTime` sin zona, `DATETIME2(0)` |
| Tildes | Sin escapar | `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` |

Diferencias que quedan y no afectan a los clientes:

- `Content-Type: application/json; charset=utf-8` en lugar de `application/json;charset=UTF-8`.
- Las rutas que no existen o un cuerpo que no es JSON responden con el formato propio de cada plataforma, como ya pasaba en Java (Payara responde HTML).

### H9. Cadena de conexión

- **Java:** `db.properties` versionado, con las credenciales del contenedor de pruebas (`root`/`root`).
- **.NET:** `appsettings.json` con la cadena del contenedor de `03. BDD/docker-compose.yml` (usuario `sa` y su clave de pruebas, que no es una clave real), y `ConnectionStrings__ApiPaisesDB` o `appsettings.Development.json` (fuera de git) para otra base.

### H10. Prueba 502 sin `asadmin`

- **Java:** el script cambia las propiedades del sistema de Payara con `asadmin`.
- **.NET:** las URL de las APIs se leen en cada petición de `CountryApi:RestCountriesUrl` y `CountryApi:UniversidadesUrl`, y el servidor carga además un `appsettings.Pruebas.json` opcional, fuera de git, que se recarga solo.
- `04. SERVIDOR/WS_COUNTRYAPI_DOTNET_RESTFul/pruebas/curl_countryapi.sh` es una copia del script de Java con dos cambios: la URL base por defecto (`http://localhost:5088/api`) y la prueba 502, que escribe ese archivo, espera la recarga, prueba y lo borra. El script de TI.2.1 no se toca.
- Para correr el script original de TI.2.1 contra .NET: `ASADMIN=/no/existe BASE=http://localhost:5088/api bash ...` (omite su sección 502, que solo sirve para Payara).

### H11. Clientes asíncronos

- **Java:** los controladores son síncronos y corren en un hilo aparte (`SwingWorker`, hilo de Android).
- **`DC_Clientes` .NET:** los controladores devuelven `Task` y los servicios `Task<RespuestaDTO<T>>`; las vistas son síncronas (`LeerPais(): Pais`, `ConfirmarEliminacion(): bool`).
- **Decisión:** controladores y servicios con `async/await`, y las interfaces de vista síncronas, como en el diagrama.
  - En WPF los diálogos modales (`ShowDialog`) ya son síncronos.
  - En MAUI los diálogos son asíncronos: el controlador corre fuera del hilo principal y la vista espera la respuesta del usuario, igual que Android (E2).

### H12. Nombres

- **Servicios web:** `LoginWebService`, `PaisWebService`, ... del `DC_Servidor` .NET (controladores de ASP.NET con `[Route("api/Login")]`, ...). Métodos en PascalCase (`Listar`, `Registrar`, `ConsultarExterno`).
- **Espacios de nombres:** `ec.edu.monster.ws`, `ec.edu.monster.controladores`, ..., como los paquetes Java. Carpetas planas en la raíz del proyecto, con `pruebas/` al mismo nivel, como pide el profesor.
- **Interfaces de vista de los clientes:** se llaman como en el diagrama (`LoginVista`, `PaisVista`, ...), sin el prefijo `I`.
- **`ConfigServidor`:** `TIMEOUT_SEGUNDOS = 15`, del diagrama .NET; en Java era `TIMEOUT_MS = 15000`.

### H13. Una transacción y un `SaveChanges` en la importación

El DS .NET dice «por cada país: INSERT o UPDATE (SaveChangesAsync)». **Decisión:** como pide el prompt y como hace Java con `executeBatch`, se acumulan todos los cambios y se guarda con **un** `SaveChanges` dentro de la transacción. EF Core los envía en lotes. El resultado es el mismo.

### H14. Base de datos en Docker

No hay SQL Server instalado. `03. BDD/docker-compose.yml` levanta SQL Server 2022 (puerto 1433, zona `America/Guayaquil` como en TI.2.1) y un servicio de un solo uso que espera a que el motor responda y corre `init.sql` y después `seed.sql` con `sqlcmd`. Se comprueba que las tildes queden bien («América»).

### H15. Commits

El prompt dice «no hagas commits», pero el autor pidió commits por funcionalidad (24/11/2026). Se hacen en `main` del repositorio de TI.2.2: uno por funcionalidad terminada y verificada, no por archivo.
