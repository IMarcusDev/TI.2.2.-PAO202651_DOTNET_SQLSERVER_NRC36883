# Plan de migración de CountryAPI a .NET + SQL Server (TI.2.2)

CountryAPI ya está implementado y aceptado en Java + MySQL (TI.2.1). Este plan describe cómo traducirlo a .NET archivo por archivo, sin cambiar el comportamiento: mismos endpoints, JSON, mensajes, códigos HTTP, validaciones, pantallas y flujos.

- **Comportamiento:** manda el código de TI.2.1 y su `DUDAS.md` (A1 a G7).
- **Tecnología y nombres .NET:** mandan `01. UML` (ERS, ECUD, `DC_Servidor`, `DC_Clientes`, DS, DA, ARQ), `02. MER` y `03. BDD` de TI.2.2.
- **Choques y decisiones:** en [DUDAS.md](DUDAS.md), sección H en adelante.

## 1. Base de datos `ApiPaisesDB` (SQL Server 2022)

| Tabla | Columnas | Restricciones |
| --- | --- | --- |
| `Usuarios` | IdUsuario, Username, PasswordHash CHAR(64), Nombre, Estado | Username UNIQUE; Estado `ACTIVO`/`INACTIVO` |
| `Regiones` | IdRegion, Nombre | Nombre UNIQUE; 6 filas |
| `Paises` | Id, Nombre, Capital, IdRegion, SubRegion, Poblacion BIGINT, Area FLOAT, Moneda, FechaActualizacion DATETIME2(0) | Nombre UNIQUE; CHECK Poblacion y Area ≥ 0; FK a Regiones |
| `Universidades` | Id, Nombre, Dominio, SitioWeb, Pais, FechaActualizacion DATETIME2(0) | UNIQUE (Nombre, Pais) |

- `init.sql` y `seed.sql` no se tocan. El seed tiene los mismos datos que el de TI.2.1.
- Sin SQL Server local: se agrega `03. BDD/docker-compose.yml` con `mcr.microsoft.com/mssql/server:2022-latest` (la imagen ya está descargada) y un servicio que corre `init.sql` y después `seed.sql` con `sqlcmd`.
- SQL Server no tiene `ON UPDATE CURRENT_TIMESTAMP`: el servidor asigna `FechaActualizacion` al registrar y al actualizar, como dice el comentario de `init.sql`.
- **Intercalación.** MySQL (`utf8mb4_0900_ai_ci`) no distingue mayúsculas ni tildes; la intercalación por defecto de SQL Server (`SQL_Latin1_General_CP1_CI_AS`) sí distingue tildes. Todas las comparaciones por texto que en Java hace MySQL se hacen con `EF.Functions.Collate(..., "Latin1_General_CI_AI")` (ver H3 a H6).

## 2. Contrato REST (idéntico al de Java)

**URL base:** `http://<host>:5088/api` (Java: `http://<host>:8080/WS_COUNTRYAPI_JAVA_RESTFul/api`).

| Método y ruta | `datos` si sale bien | Mensajes y códigos |
| --- | --- | --- |
| `POST /Login/login` | `{token, nombre}` | 200 «Ingreso de sesión exitoso» · 401 «Credenciales incorrectas» · 500 «Servicio no disponible, intente más tarde» |
| `POST /Login/logout` | `null` | 200 «Sesión cerrada», aunque el token no exista |
| `GET /Paises[?nombre=]` | lista ordenada por nombre | 200 «Consulta realizada correctamente» |
| `POST /Paises` | país con su id | 201 «País registrado correctamente» · 400 · 409 «Ya existe un país con ese nombre» |
| `PUT /Paises/{id}` | país | 200 «País actualizado correctamente» · 400 · 404 «El país no existe» · 409 |
| `DELETE /Paises/{id}` | `null` | 200 «País eliminado correctamente» · 404 |
| `GET /Paises/buscar?nombre=&capital=&region=` | lista | 200 «Búsqueda realizada correctamente» · 400 «Región no válida» |
| `POST /Paises/importar` | `{recibidos, registrados, actualizados}` | 200 «Importación finalizada» · 502 · 500 |
| `GET /Paises/externo/{nombre}` | lista con `guardado` | 200 «Consulta en la API exitosa» · 404 · 502 · 400 «Ingrese al menos dos caracteres» |
| `POST /Paises/guardar` | país | 200 «País guardado correctamente» · 400 |
| `GET /Reportes` | `{paisesPorRegion, paisMasPoblado, promedioPorRegion}` | 200 «Reportes generados correctamente» · 404 «No hay países registrados para generar reportes» |
| `POST /Universidades/importar?pais=` | resumen | 200 «Importación de universidades finalizada» · 404 · 502 · 400 «Ingrese el país» |
| `GET /Universidades?pais=` | lista ordenada por nombre | 200 «Consulta realizada correctamente» · 400 |

Sin token válido, todo salvo `login` y `logout` responde 401 «Token inválido o sesión expirada». Cualquier error no previsto responde 500 «Ocurrió un error inesperado, intente nuevamente».

**Formato del JSON, comprobado contra el servidor Java desplegado (24/11/2026):**

- Claves en camelCase y en el orden de Java (`id, nombre, capital, region, subregion, poblacion, area, moneda, guardado`).
- `estado` como texto (`"OK"` / `"ERROR"`).
- `"datos": null` sí se escribe; dentro de `datos`, los campos nulos se omiten (un país sin capital no trae `capital`).
- Los `double` salen como `Double.toString` de Java: `276841.0`, `0.5`, `1.7098242E7`.
- Fechas como `"2026-11-23T19:49:26"` (sin zona ni fracción).
- Tildes sin escapar (`"América"`, no `"Am\u00e9rica"`).

## 3. Servidor: `04. SERVIDOR/WS_COUNTRYAPI_DOTNET_RESTFul`

ASP.NET Core Web API en Kestrel :5088 (`0.0.0.0`), EF Core con SQL Server, `Newtonsoft.Json` para las APIs externas y `System.Text.Json` para el contrato. Espacio de nombres raíz `ec.edu.monster`, carpetas planas (`ws/`, `controladores/`, ...) y `pruebas/` al mismo nivel (proyecto xUnit excluido del principal).

| Java (TI.2.1) | C# (TI.2.2) | Notas |
| --- | --- | --- |
| `config/ApplicationConfig` | `Program.cs` | Inyección de dependencias, JSON, rutas, puerto |
| `config/DBConfig` + `db.properties` | `config/AppDbContext.cs` + `ConnectionStrings:ApiPaisesDB` en `appsettings.json` | Mapeo de las 4 tablas en `OnModelCreating` |
| `ws/LoginWebService` | `ws/LoginWebService.cs` | `[Route("api/Login")]`; `Login`, `Logout` |
| `ws/PaisWebService` | `ws/PaisWebService.cs` | `Listar`, `Registrar`, `Actualizar`, `Eliminar`, `Buscar`, `Importar`, `ConsultarExterno`, `Guardar` |
| `ws/ReporteWebService` | `ws/ReporteWebService.cs` | `Generar` |
| `ws/UniversidadWebService` | `ws/UniversidadWebService.cs` | `Importar`, `Listar` |
| `controladores/*Controlador` (4) | `controladores/*Controlador.cs` (4) | Mismas reglas y mensajes |
| `servicios/TokenService` | `servicios/TokenService.cs` | Singleton, `ConcurrentDictionary`, reloj inyectable para pruebas |
| `servicios/HashService` | `servicios/HashService.cs` | |
| `servicios/ConsumoAPI` | `servicios/ConsumoAPI.cs` | `HttpClient` (15 s) + `Newtonsoft.Json`; v5 con clave |
| `servicios/ConsumoAPIUniversidades` | `servicios/ConsumoAPIUniversidades.cs` | Hasta 3 redirecciones |
| `modelos/interfaces/I*DAO` (4) | `modelos/interfaces/I*DAO.cs` (4) | |
| `modelos/repositorios/*DAO` (4) | `modelos/repositorios/*DAO.cs` (4) | Reciben `AppDbContext`; LINQ parametrizado |
| `modelos/data/*` (5) | `modelos/data/*.cs` (5) | `Pais`, `Region`, `Usuario`, `Universidad`, `Sesion` |
| `modelos/dto/*` (8) | `modelos/dto/*.cs` (8) | `RespuestaDTO<T>` con `CodigoHttp(Exception)` |
| `modelos/enums/*` (2) | `modelos/enums/*.cs` (2) | |
| — | `config/DoubleFormatoJava.cs` | Convertidor de `double` al formato de Java (H8) |
| `pruebas/curl_countryapi.sh` | `pruebas/curl_countryapi.sh` | Copia adaptada: solo cambia la URL base y la prueba 502 (H11) |
| `src/test/...` (7 clases, 54 pruebas) | `pruebas/WS_COUNTRYAPI_DOTNET_RESTFul.Pruebas/` (xUnit) | Las mismas 54 pruebas |

**Excepciones → HTTP** (`RespuestaDTO.CodigoHttp`): `ArgumentException` 400, `UnauthorizedAccessException` 401, `KeyNotFoundException` 404, `InvalidOperationException` 409, `HttpRequestException` / `TaskCanceledException` / `IOException` 502, errores de base de datos 500 «Servicio no disponible, intente más tarde», el resto 500 «Ocurrió un error inesperado, intente nuevamente». Sin clases de excepción propias.

**Importación de países** (como Java): API primero (502 sin tocar la base) → se cargan una vez los existentes (clave sin tildes ni mayúsculas) → nuevos con `AddRange` y existentes actualizados → un `SaveChanges` dentro de `BeginTransaction` → rollback y 500 si algo falla. Repetidos en la API: se guarda el primero.

**Configuración:**

| Java | .NET |
| --- | --- |
| `db.properties` | `ConnectionStrings:ApiPaisesDB` (`appsettings.json`, o la variable `ConnectionStrings__ApiPaisesDB`) |
| `api.properties` / `COUNTRYAPI_RESTCOUNTRIES_CLAVE` | La misma variable de entorno, o `CountryApi:RestCountriesClave` en `dotnet user-secrets`. Nunca en archivos versionados |
| propiedades `countryapi.restcountries.url` y `countryapi.universidades.url` | `CountryApi:RestCountriesUrl` y `CountryApi:UniversidadesUrl`, leídas en cada petición (`appsettings*.json` se recargan solos) |

## 4. Clientes: estructura común (`DC_Clientes` .NET)

Como en Java (D1, E1, F1, G1), las capas `modelos`, `servicios`, `controlador`, `config` y las interfaces de `vista` se traducen **una vez** y se copian a los cuatro clientes; solo cambian las vistas.

| Java | C# |
| --- | --- |
| `modelos/*` (11) | `modelos/*.cs` (11): `Pais`, `CriteriosBusqueda`, `Universidad`, `LoginRequest`, `LoginResponse`, `ResumenImportacion`, `ConteoRegion`, `PromedioRegion`, `Reporte`, `RespuestaDTO<T>`, `EstadoRespuesta` |
| `servicios/ClienteRest` (`HttpURLConnection` + Gson) | `servicios/ClienteRest.cs` (`HttpClient` + `Newtonsoft.Json`) |
| `servicios/SesionServicio`, `LoginServicio`, `PaisServicio`, `ReporteServicio`, `UniversidadServicio` | `servicios/*.cs` con los métodos `Task<RespuestaDTO<T>>` del diagrama |
| `controlador/*Controlador` (5) + `ValidadorDatos` | `controlador/*.cs`, métodos `Task` |
| `config/ConfigServidor` (`URL_BASE`, `TIMEOUT_MS`) | `config/ConfigServidor.cs` (`URL_BASE`, `TIMEOUT_SEGUNDOS = 15`, del diagrama .NET) leída de `appsettings.json` junto al ejecutable |
| `vista/VistaBase` + 8 interfaces | `vista/*.cs`: mismas interfaces y métodos (`MostrarMensaje`, `LeerPais`, ...) |

Pruebas de cada cliente: xUnit en `pruebas/` (proyecto aparte, excluido del `.csproj` principal), con `pruebas/dobles/VistaFalsa` y un `pruebas/dobles/ServidorFalso` HTTP real (`TcpListener` en un puerto libre), igual que el `HttpServer` de las pruebas Java.

### 4.1 Escritorio WPF: `04. CLIESC/ESC_COUNTRYAPI_DOTNET_RESTFul`

| Java (Swing) | C# (WPF) |
| --- | --- |
| `App` | `App.xaml` / `App.xaml.cs` |
| `vista/VentanaPrincipal` (implementa `MenuVista`) | `vista/VentanaPrincipal.xaml(.cs)` |
| `vista/LoginPanel`, `MenuPanel`, `PaisPanel`, `BusquedaPanel`, `ConsultaApiPanel`, `ImportacionPanel`, `ReportePanel`, `UniversidadPanel` | `vista/*Panel.xaml(.cs)` (`UserControl` que implementa su interfaz) |
| `vista/PanelBase` | `vista/PanelBase.cs` |
| `vista/FormularioPaisDialogo` | `vista/FormularioPaisDialogo.xaml(.cs)` (ventana modal) |
| `vista/Estilos` | `vista/Estilos.xaml` (diccionario de recursos con la paleta) |
| `vista/Alerta`, `IndicadorServidor` | `vista/Alerta.xaml`, `vista/IndicadorServidor.xaml` |
| `vista/TablaModelo` (`JTable`) | `DataGrid` con columnas declaradas en XAML; formatos en `vista/Estilos.cs` |
| `JOptionPane.showConfirmDialog` | `vista/DialogoConfirmacion.xaml(.cs)` con el estilo de la aplicación |
| `servidor.properties` (`-Dcountryapi.config`) | `appsettings.json` junto al `.exe` (variable `COUNTRYAPI_CONFIG`) |
| 3 clases de prueba (49) | `pruebas/` xUnit (49): `ValidadorDatosTest`, `ServiciosTest`, `ControladoresTest` y `pruebas/dobles/` |

### 4.2 Móvil MAUI: `04. CLIMOV/MOV_COUNTRYAPI_DOTNET_RESTFul`

| Java (Android) | C# (MAUI) |
| --- | --- |
| `AplicacionCountryApi` | `AplicacionCountryApi.cs` (cliente, sesión y servicios), `App.xaml(.cs)`, `MauiProgram.cs` |
| `PrincipalActivity` (implementa `MenuVista`) | `vista/PaginaPrincipal.xaml(.cs)`: barra azul, pantalla actual, progreso, bloqueo y capa de diálogos |
| `vista/*Fragment` (8) | `vista/*Pantalla.xaml(.cs)` (`ContentView` que implementa su interfaz) |
| `FragmentoBase` | `vista/PantallaBase.cs` |
| `FormularioPais`, `PaisAdaptador`, `Tabla`, `Formato`, `Hilos`, `IndicadorServidor` | Mismo nombre en `vista/` |
| `res/layout/aviso.xml`, `TextInputLayout`, `LinearProgressIndicator` | `vista/Aviso.cs`, `vista/Campo.cs`, `vista/BarraProgreso.cs` |
| `res/values` (colores y estilos), `res/drawable` | `Resources/Styles/Estilos.xaml`, `vista/Colores.cs`; íconos SVG en `Resources/Images` |
| `ConfigServidor` (`BuildConfig`, `SharedPreferences`) | `config/ConfigServidor.cs` (propiedad `Servidor` del `.csproj`) y `config/ConfigServidor.Preferencias.cs` (`Preferences`); por defecto `http://10.0.2.2:5088/api` |
| 4 clases de prueba (65) | `pruebas/` xUnit (`net10.0`) que enlaza los `.cs` sin MAUI (65) |

Se compila por un junction corto (`C:\climovcountry`), por el límite de 260 caracteres de aapt2.

### 4.3 Consola: `04. CLICON/CONSOLE_COUNTRYAPI_DOTNET_RESTFul`

| Java | C# |
| --- | --- |
| `App` | `Program.cs` (clase `Program`: arma el cliente y recorre login → menú) |
| `vista/*Consola` (8), `Consola`, `PantallaConsola`, `Tabla` | `vista/*.cs` con los mismos nombres |
| `servidor.properties` (`-Dcountryapi.config`) | `appsettings.json` junto al ejecutable (variable `COUNTRYAPI_CONFIG`), como el escritorio |
| 4 clases de prueba (64) | `pruebas/` xUnit (64): las 49 del escritorio y `ConsolaTest` (15) |

### 4.4 Web MVC: `04. CLIWEB/WEB_COUNTRYAPI_DOTNET_RESTFul`

| Java (JSP + Servlets) | C# (ASP.NET Core MVC) |
| --- | --- |
| `web/*Servlet` (10) | `web/*Controller.cs` (Login, Logout, Menu, Paises, Buscar, Consultar, Importar, Reportes, Universidades, Estado) |
| `web/ModulosSesion`, `Pagina`, `AplicacionWeb` | `web/ModulosSesion.cs` (vistas y controladores por sesión), `web/Pagina.cs`, `Program.cs` |
| `HttpSession` del contenedor | `web/AlmacenSesiones.cs` (sesión en el servidor con cookie `HttpOnly`, 15 min, cambio de id e invalidación) |
| `vista/*VistaWeb` (8) + `VistaWeb` | `vista/*VistaWeb.cs` |
| `filtros/AuthFilter` | `filtros/AuthFilter.cs` (filtro de acción) |
| `filtros/CsrfFilter` | `filtros/CsrfFilter.cs`: filtro global con el Antiforgery de ASP.NET Core (campo `_csrf`); login vencido → formulario nuevo, el resto → 403 |
| `WEB-INF/vista/*.jsp`, `partes/*.jspf` | `Views/*.cshtml`, `Views/Shared/_Layout.cshtml` y parciales |
| `css/countryapi.css`, `js/countryapi.js`, `globo.svg` | `wwwroot/` (mismos archivos) |
| Sesión de 15 min y cambio de id al iniciar sesión | Igual, con `AlmacenSesiones` |
| `servidor.properties` dentro del war (`-Dcountryapi.config`) | `appsettings.json` (`Servidor:Url`, variable `COUNTRYAPI_CONFIG`); el sitio escucha en `0.0.0.0:5090` |
| 4 clases de prueba (69) | `pruebas/` xUnit (69): las 49 del escritorio y `WebTest` (20) con `WebApplicationFactory` y `dobles/Navegador` |

## 5. Fases (me detengo al final de cada una)

| Fase | Entregable | Verificación |
| --- | --- | --- |
| 0 | `PLAN.md`, `DUDAS.md`, `CLAUDE.md` | Tu confirmación |
| 1 | BDD en Docker + servidor + pruebas | `dotnet build` y `dotnet test`; suite curl de Java contra :5088 con todos los casos OK; JSON comparado con el servidor Java; orden de los 254 nombres igual que en MySQL; prueba cruzada consola Java → servidor .NET |
| 2 | Escritorio WPF | `dotnet build`, `dotnet test` (49), revisión visual |
| 3 | Móvil MAUI | `dotnet build -f net10.0-android`, `dotnet test` (65), emulador |
| 4 | Consola | `dotnet test` (64); prueba cruzada consola .NET → servidor Java |
| 5 | Web MVC | `dotnet test` (69), revisión en el navegador |

**Commits** (pedido del autor): uno por funcionalidad, no por archivo. Por ejemplo, en la fase 1: base de datos, servidor, pruebas y suite adaptada.

## 6. Entorno detectado (24/11/2026)

- **.NET:** SDK 10.0.401. Runtimes: `Microsoft.NETCore.App` 8.0.21 y 10.0.12; `AspNetCore.App` y `WindowsDesktop.App` **solo 10.0.12**. Cargas de trabajo `android` y `maui-windows` de .NET 10. Ver **H1**.
- **SQL Server:** no hay servicio local. Docker Desktop 29.9.2 con la imagen `mssql/server:2022-latest`. `sqlcmd` (ODBC 17) instalado. El puerto 1433 está libre.
- **TI.2.1 en marcha:** Payara con `WS_COUNTRYAPI_JAVA_RESTFul` en :8080 y `countryapi-mysql` con 254 países importados. Sirve para comparar respuestas y el orden sin gastar cuota de REST Countries.
- `pandoc` disponible.
