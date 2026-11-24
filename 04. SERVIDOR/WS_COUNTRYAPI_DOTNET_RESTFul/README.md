# WS_COUNTRYAPI_DOTNET_RESTFul

Servidor RESTful de CountryAPI (TI.2.2). Expone la API que consumen los cuatro clientes, consume las APIs públicas de países y de universidades, y guarda los datos en SQL Server.

Es la traducción del servidor Java de TI.2.1: mismos endpoints, mismo JSON, mismos mensajes y códigos HTTP. Un cliente Java funciona contra este servidor sin cambios, salvo la URL.

- .NET 10, ASP.NET Core Web API en Kestrel.
- EF Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`) con DAO, y `Newtonsoft.Json` para las APIs externas.
- URL base: `http://<servidor>:5088/api` (escucha en todas las interfaces, también para el celular).

## 1. Base de datos

Desde la carpeta `03. BDD` del proyecto:

```bash
docker compose up -d          # SQL Server 2022 con ApiPaisesDB creada y cargada (puerto 1433)
docker compose ps -a          # «countryapi-sqlserver-init» debe terminar con Exited (0)
docker compose down -v && docker compose up -d   # volver a los datos del seed
```

La conexión se configura en `appsettings.json`, en `ConnectionStrings:ApiPaisesDB`. El valor por defecto es el del contenedor de pruebas:

```
Server=localhost,1433;Database=ApiPaisesDB;User Id=sa;Password=CountryApi#2026;TrustServerCertificate=True
```

Para usar otra base sin tocar el archivo versionado, use la variable de entorno `ConnectionStrings__ApiPaisesDB` o un `appsettings.Development.json` (no se sube a git).

## 2. Clave de REST Countries v5

La versión v3.1 de REST Countries que describe la documentación fue retirada. El servidor usa la **v5**, que exige una clave gratuita (https://restcountries.com/api-keys, plan Free con 1000 peticiones al mes). Guárdela de una de estas dos formas, nunca en un archivo versionado:

```bash
dotnet user-secrets set CountryApi:RestCountriesClave <su-clave>    # desde la carpeta del proyecto
# o bien
set COUNTRYAPI_RESTCOUNTRIES_CLAVE=<su-clave>                     # variable de entorno (PowerShell: $env:COUNTRYAPI_RESTCOUNTRIES_CLAVE="...")
```

Sin clave, la importación y la consulta de países responden 502; el resto del sistema funciona igual. Cada importación usa 3 peticiones y cada consulta por nombre, 1.

## 3. Ejecutar

```bash
dotnet run          # http://0.0.0.0:5088/api
```

También se puede abrir `WS_COUNTRYAPI_DOTNET_RESTFul.csproj` en Visual Studio y ejecutar el perfil `http`.

## 4. Pruebas

**Unitarias (xUnit, sin base ni internet),** en `pruebas/`:

```bash
dotnet test pruebas
dotnet test pruebas --filter "FullyQualifiedName~TokenServiceTest"     # una clase
```

Son las 54 pruebas del servidor Java traducidas una a una (`HashService`, `TokenService`, el mapeo del JSON de `ConsumoAPI` y `ConsumoAPIUniversidades` con un servidor HTTP falso, los códigos HTTP de `RespuestaDTO` y las reglas de los controladores con DAO en memoria), más 14 casos de `DoubleFormatoJava`, que escribe los decimales igual que Java. Detenga el servidor antes de correrlas: si está en marcha, bloquea su `.dll` y la compilación falla.

**De integración, con el servidor en marcha** (desde Git Bash):

```bash
bash pruebas/curl_countryapi.sh
```

Es la suite `curl` de Java con un `curl` por endpoint, incluidos los casos 400, 401, 404, 409 y 502. Solo cambian la URL por defecto y la prueba 502: el script escribe `appsettings.Pruebas.json` con las APIs apuntando a un puerto sin servicio, el servidor lo recarga solo, y al terminar lo borra.

La suite original de TI.2.1 también pasa contra este servidor (omitiendo su prueba 502, que usa `asadmin`):

```bash
ASADMIN=/no/existe BASE=http://localhost:5088/api bash "../../../TI.2.1. PAO202651_JAVA_MYSQL_NRC36883/04. SERVIDOR/WS_COUNTRYAPI_JAVA_RESTFul/pruebas/curl_countryapi.sh"
```

## 5. Configuración opcional

| Clave | Uso |
| --- | --- |
| `CountryApi:RestCountriesUrl` | Dirección de REST Countries (por defecto `https://api.restcountries.com/countries/v5`) |
| `CountryApi:RestCountriesClave` | Clave de REST Countries (`dotnet user-secrets`); si falta, se usa `COUNTRYAPI_RESTCOUNTRIES_CLAVE` |
| `CountryApi:UniversidadesUrl` | Dirección de la API de universidades (por defecto `http://universities.hipolabs.com`) |
| `Urls` | Dirección y puerto de Kestrel (por defecto `http://0.0.0.0:5088`) |

Las direcciones de las APIs se leen en cada petición; los cambios en `appsettings*.json` se aplican sin reiniciar.

## 6. Diferencias con la versión Java

Están explicadas en `DUDAS.md` (sección H). Las principales:

- **Tildes:** SQL Server distingue tildes por defecto y MySQL no; las búsquedas e igualdades usan la intercalación `Latin1_General_CI_AI` para dar los mismos resultados («peru» encuentra «Perú»).
- **JSON:** los decimales se escriben como en Java (`276841.0`, `1.7098242E7`) y los campos nulos dentro de `datos` se omiten. Las respuestas se compararon byte a byte con las del servidor Java, con los mismos 254 países.
