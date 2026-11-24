# WEB_COUNTRYAPI_DOTNET_RESTFul

Cliente web (ASP.NET Core MVC + Razor) de CountryAPI. Consume **solo** el servidor RESTful `WS_COUNTRYAPI_DOTNET_RESTFul`; nunca llama a SQL Server ni a las APIs públicas.

- .NET 10. `Newtonsoft.Json` 13.0.4 para el JSON.
- Es la traducción del cliente JSP + Servlets de TI.2.1: mismas pantallas, rutas, textos y mensajes.
- Diseño de referencia:
  - barra superior azul con navegación, usuario e indicador del servidor;
  - menú de tarjetas;
  - tablas con encabezado celeste;
  - avisos verdes y rojos.
- La página se adapta a pantallas pequeñas.
- Todo funciona sin JavaScript. El JS solo actualiza el indicador cada 10 segundos y envía el filtro de región al elegirlo.

## Configuración

`appsettings.json`:

```json
{
  "Urls": "http://0.0.0.0:5090",
  "Servidor": { "Url": "http://localhost:5088/api" }
}
```

- `Urls` es la dirección del cliente web. Con `0.0.0.0` también responde desde otras PC de la red.
- `Servidor:Url` es la dirección del servidor RESTful. Para cambiarla sin recompilar, edite el `appsettings.json` que queda junto al ejecutable, o indique otro archivo con la variable de entorno `COUNTRYAPI_CONFIG`, igual que en escritorio y consola.

## Compilar y ejecutar

Requisitos:

- el servidor en marcha (ver `04. SERVIDOR/WS_COUNTRYAPI_DOTNET_RESTFul/README.md`);
- la base levantada con `03. BDD/docker-compose.yml`.

```bash
dotnet run
```

Abra <http://localhost:5090/>. Usuarios de prueba: `monster` / `monster9` y `admin` / `admin123`.

## Pruebas

```bash
dotnet test pruebas
dotnet test pruebas --filter "FullyQualifiedName~WebTest"                 # una clase
dotnet test pruebas --filter "FullyQualifiedName~WebTest+Paises"          # un grupo
```

Las 69 pruebas (las mismas de Java) son xUnit y no necesitan Kestrel ni el servidor real:

- **`ValidadorDatosTest`, `ServiciosTest` y `ControladoresTest`:** las mismas del cliente de escritorio, contra un servidor HTTP falso.
- **`WebTest`:** el sitio completo (filtros, controladores MVC y vistas Razor) con `WebApplicationFactory` y un navegador en memoria (`dobles/Navegador`) que guarda las cookies y envía el token CSRF de la página. El cliente web apunta al servidor falso. Cubre:
  - inicio y cierre de sesión;
  - CSRF y control de acceso;
  - el formulario de país que se reabre con lo escrito (validación y 409);
  - eliminar solo con confirmación;
  - guardar desde la consulta a la API sin repetir la consulta;
  - la sesión expirada.

## Seguridad

- **Token en la sesión.** El token del servidor vive en la sesión de cada usuario, en el servidor web, dentro de su `SesionServicio`; nunca llega al navegador.
- **Cookie de sesión.** `CountryAPI.Sesion` es `HttpOnly`, solo por cookie, y caduca a los 15 minutos sin uso, igual que el token. Al iniciar sesión se cambia el id de sesión, y al cerrarla se invalida.
- **`AuthFilter`.** Sin sesión, las pantallas protegidas redirigen a `/login`.
- **`CsrfFilter`.** Todo POST lleva el campo `_csrf` (Antiforgery de ASP.NET Core). Sin él se responde 403; en el inicio de sesión, en cambio, se muestra un formulario nuevo.
- **Errores.** Razor escapa los textos. Los errores 403, 404 y 500 muestran una página propia, sin trazas.

## Estructura (diagrama DC_Clientes)

```
├── Program.cs     Arranque: ClienteRest compartido, sesiones, CSRF, páginas de error
├── config/        ConfigServidor (URL_BASE desde appsettings.json, TIMEOUT_SEGUNDOS = 15)
├── modelos/       Pais, Reporte, Universidad, RespuestaDTO<T>, ...      (iguales al escritorio)
├── servicios/     ClienteRest (HttpClient + Newtonsoft.Json), SesionServicio y un servicio por módulo
├── controlador/   Login, Pais, Api, Reporte, Universidad y ValidadorDatos (iguales al escritorio)
├── vista/         Interfaces del diagrama y su implementación web (LoginVistaWeb, PaisVistaWeb, ...)
├── web/           Controladores MVC (uno por pantalla), ModulosSesion, Pagina y AlmacenSesiones
├── filtros/       AuthFilter y CsrfFilter
├── Views/         Una vista Razor por pantalla, el marco común (Shared/_Layout) y los avisos
├── wwwroot/       countryapi.css (paleta de referencia) y countryapi.js (indicador y filtros)
└── pruebas/       Proyecto xUnit aparte (excluido del .csproj principal), con dobles/
```

**Cómo encajan los controladores de escritorio con la web:**

- Cada usuario tiene en su sesión sus vistas y controladores (`ModulosSesion`).
- En cada petición, el controlador MVC enlaza las vistas con la petición y llama al controlador de la aplicación:
  - lo que el controlador «muestra» queda en `HttpContext.Items` y lo dibuja la vista Razor;
  - lo que «lee» sale de los campos del formulario o de la dirección.
- Si el controlador reabre el formulario de país por un error, `PaisVistaWeb` lo deja pendiente con lo escrito y la página lo vuelve a mostrar con el mensaje.
- Después de un POST que funcionó, se redirige (POST/Redirect/GET) y el aviso pasa la redirección guardado en la sesión.

## Pantallas

| Ruta | Pantalla |
|---|---|
| `/login` | Inicio de sesión |
| `/menu` | Menú de tarjetas (orden del ECUD1) |
| `/importar` | Importar países, con panel de confirmación y resumen |
| `/consultar` | Consultar país en la API; «Guardar» por fila |
| `/paises` | Lista, búsqueda por nombre, nuevo, editar y eliminar (con confirmación) |
| `/buscar` | Buscar y filtrar: nombre, capital y región; la región vuelve a consultar al elegirla |
| `/reportes` | País más poblado, países por región y promedio poblacional |
| `/universidades` | Importar y ver las universidades de un país (en inglés) |
| `/estado` | JSON del indicador del servidor |
