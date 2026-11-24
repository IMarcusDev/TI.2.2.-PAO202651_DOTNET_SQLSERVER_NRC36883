# Dudas y decisiones de la migración a .NET (CountryAPI · TI.2.2)

Las decisiones A1 a G7 de TI.2.1 (`../TI.2.1. PAO202651_JAVA_MYSQL_NRC36883/DUDAS.md`) siguen vigentes: el comportamiento es el del código Java. Aquí solo se registra lo que cambia por la tecnología, con el mismo formato: dónde está el choque, las opciones y la recomendación.

## H. Fase 0 (lectura y plan)

### H1. Versión de .NET

> **Resuelto el 24/11/2026 con la opción a.** El autor aceptó las recomendaciones H2 a H15 y eligió .NET 10 (`net10.0`, EF Core 10) para los cinco proyectos.

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
- **Decisión (aplicada en la fase 1):** para dar el mismo 409 sin cambiar la base, `PaisControlador.ActualizarPais` comprueba antes de actualizar, con los métodos del diagrama:
  1. `BuscarPorId`; si no existe, 404;
  2. si el nombre cambió (comparado sin tildes ni mayúsculas) y `ExistePorNombre` lo encuentra, 409;
  3. `Actualizar`.

  Es el mismo orden que el DS .NET (`DS_Gestionar_Paises`) y da los mismos códigos que Java en todos los casos: renombrar «Perú» a «peru» funciona, y renombrar otro país a «Peru» es 409. Si aun así el UNIQUE salta (otro usuario guardó al mismo tiempo), la violación de clave (2601/2627) se convierte en 409, como el `SQLIntegrityConstraintViolationException` de Java.

### H5. Comodines de `LIKE`

Java escapa `\`, `%` y `_`. En SQL Server `[` también es comodín (`[a-z]`), y en MySQL no. **Decisión:** escapar además `[` para que «[» se busque literalmente, igual que en Java. Se usa `\` como carácter de escape.

### H6. Orden por nombre

`ORDER BY nombre` en MySQL (`0900_ai_ci`) y en SQL Server podría diferir con guiones, apóstrofos o letras con tilde.

> **Resuelto el 24/11/2026 sin cambios.** Los 254 nombres de la MySQL de TI.2.1 (importados de la API, con tildes, guiones y espacios) salen en el mismo orden con el `ORDER BY Nombre` de SQL Server y su intercalación por defecto. El orden se deja en la base.

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

## I. Fase 1 (servidor)

Solo lo que tuvo que cambiar respecto de Java por la tecnología; ninguna decisión cambia el contrato.

- **I1. Equivalencia comprobada, no supuesta.**
  - **Comparación byte a byte:** con los mismos 254 países y 43 universidades copiados de la MySQL de TI.2.1 (mismos id y fechas), 40 peticiones dan exactamente el mismo código y el mismo cuerpo en los dos servidores. Cubren listados, búsquedas con tildes y comodines, filtros, reportes, universidades y todos los errores.
  - **Suite de TI.2.1 sin modificar:** 38 de 38 casos (omite su 502, que usa `asadmin`).
  - **Copia adaptada:** 42 de 42, con los tres 502.
  - **Prueba cruzada:** la consola Java funciona contra el servidor .NET.
- **I2. Clases de excepción de .NET** (no son clases propias): las de la tabla de `PLAN.md`. Los errores de la base se reconocen por `DbException` (por ejemplo `SqlException`) o `DbUpdateException`, aunque EF Core los envuelva en `InvalidOperationException`. Se revisan antes que el resto para no confundirlos con el 409.
- **I3. Inyección de dependencias en lugar de `new`.**
  - Java conectaba las clases a mano y usaba `TokenService.getInstancia()`. Aquí `Program.cs` registra `TokenService` como singleton, los DAO y controladores por petición, y `ConsumoAPI` y `ConsumoAPIUniversidades` como clientes de `IHttpClientFactory` (15 s).
  - Las redirecciones de la API de universidades (hasta 3) las sigue el manejador HTTP; Java las seguía a mano porque `HttpURLConnection` no pasa de HTTP a HTTPS.
- **I4. Reloj de `TokenService`.** Java recibía un `Clock`; aquí es un `TimeProvider`, que las pruebas reemplazan.
- **I5. Pruebas.**
  - Las 54 de Java, traducidas a xUnit con los mismos casos.
  - 14 casos más para el convertidor de `double` (`DoubleFormatoJava`), que no existe en Java.
  - El servidor HTTP falso es un `TcpListener` mínimo, en lugar del `HttpServer` del JDK.
- **I6. Rutas sin distinguir mayúsculas.** ASP.NET Core acepta `/api/paises` además de `/api/Paises`; JAX-RS solo la segunda. Los clientes usan siempre la forma documentada, así que no cambia nada para ellos.

## J. Fase 2 (escritorio WPF)

Solo lo que tuvo que cambiar respecto de Swing por la tecnología; las pantallas, los textos, las validaciones y los mensajes son los de Java.

- **J1. Capas comunes traducidas una vez.** `modelos`, `config`, `servicios`, `controlador` y las interfaces de `vista` quedan listas para copiarse a MAUI, consola y web. Solo cambian las vistas.
- **J2. Cliente asíncrono (H11).** Servicios y controladores devuelven `Task`; las interfaces de vista siguen siendo síncronas, como en Java. `VentanaPrincipal.Ejecutar` hace lo que hacía el `SwingWorker`: muestra la barra de progreso, bloquea los clics y muestra en pantalla cualquier error inesperado sin cerrar la aplicación.
- **J3. Configuración.** `servidor.properties` pasa a `appsettings.json` (`Servidor:Url`), que se copia junto al `.exe`. `-Dcountryapi.config` pasa a la variable de entorno `COUNTRYAPI_CONFIG`. El orden de búsqueda es el mismo de Java.
- **J4. Diálogos.** `JOptionPane.showConfirmDialog` no tiene equivalente con estilo en WPF (`MessageBox` usa el aspecto del sistema). `DialogoConfirmacion` es una ventana modal con el mismo título, texto y botones, y la paleta de la aplicación.
- **J5. Tablas.** `TablaModelo` (modelo de `JTable`) desaparece: el `DataGrid` declara sus columnas en XAML y usa los convertidores `FormatoNumero` y `SiNo` de `vista/Estilos.cs`. Los números se formatean con la cultura `es-EC` (`17.643.060`, `1.285.216,25`), como el `NumberFormat` de Java.
- **J6. Filtros de región.** Los chips son `RadioButton` y vuelven a consultar con el evento `Checked`, no con `Click`. Así responden también al teclado y a la automatización de la interfaz (UI Automation). Volver a pulsar el chip que ya está elegido no repite la consulta; el resultado sería el mismo.
- **J7. Pruebas.** Las 49 de Java, traducidas a xUnit con los mismos casos, en `pruebas/` (proyecto aparte, excluido del `.csproj`). Los dobles van en `pruebas/dobles/`: `VistaFalsa` y `ServidorFalso` (`TcpListener` mínimo en lugar del `HttpServer` del JDK).
- **J8. Revisión visual.** Se recorrieron todas las pantallas contra el servidor .NET y la base del seed: inicio de sesión (con error), menú, Países, Buscar y filtrar (filtro Europa → 2 países), Reportes, Universidades, Consultar país en la API e Importar países.

## K. Fase 3 (móvil MAUI)

Solo lo que tuvo que cambiar respecto del cliente Android por la tecnología; las pantallas, los textos, las validaciones, los mensajes y la navegación son los de Java.

- **K1. Capas comunes copiadas del escritorio.** `modelos`, `servicios`, `controlador` y las interfaces de `vista` son las de la fase 2, sin cambios, salvo dos piezas que en Java también eran distintas en el móvil:
  - `ClienteRest`: la dirección se puede cambiar mientras la app corre (propiedad `UrlBase`, el `setUrlBase` de Java).
  - `ConfigServidor`: dirección de compilación más la guardada por el usuario.
- **K2. Hilos (H11).** Como en Android, cada acción del controlador corre fuera del hilo principal (`Task.Run`). Las vistas cambian sus controles con `MainThread` (`vista/Hilos.cs`), y los diálogos bloquean ese hilo de fondo hasta que el usuario responde. Así las interfaces de vista siguen siendo síncronas.
- **K3. Nombres de las pantallas.** El plan decía `vista/*Vista*.xaml`, pero ese nombre ya lo usan las interfaces del diagrama (`LoginVista`, `PaisVista`, ...). Los `Fragment` de Android pasan a ser `*Pantalla` (`LoginPantalla`, `PaisPantalla`, ...) y `FragmentoBase` pasa a `PantallaBase`. `PrincipalActivity` pasa a `PaginaPrincipal`. El resto de clases auxiliares conserva su nombre.
- **K4. Dirección del servidor.**
  - `gradle.properties` (`-Pservidor=...`) pasa a la propiedad `Servidor` del `.csproj` (`-p:Servidor=...`), que llega a la app como metadato del ensamblado.
  - `SharedPreferences` pasa a `Preferences`.
  - Por defecto, `http://10.0.2.2:5088/api`.
  - La parte que usa `Preferences` va en `ConfigServidor.Preferencias.cs`, para que las pruebas (que corren sin MAUI) compilen el resto.
- **K5. Diálogos.**
  - MAUI no tiene el `MaterialAlertDialog` de Android. El formulario de país, las confirmaciones y «Cambiar servidor» son una capa de `PaginaPrincipal`, con el mismo título, texto y botones: «atrás» o tocar fuera cancela, y un error de validación deja el diálogo abierto.
  - El menú «Editar / Eliminar» al tocar un país es el `DisplayActionSheetAsync` nativo, en lugar del `PopupMenu`.
- **K6. Controles sin equivalente directo.**
  - `TextInputLayout` pasa a `vista/Campo.cs`, con la etiqueta fija arriba en lugar de la etiqueta flotante. Conserva el ojo para ver la clave (`password_toggle`) y la «x» para borrar el texto en los campos de búsqueda (`clear_text`).
  - El `ExposedDropdownMenu` de la región pasa a un `Picker`.
  - El `LinearProgressIndicator` pasa a `vista/BarraProgreso.cs`.
  - Los chips de región son botones con los colores de `chip_*.xml`. Tocar la región ya elegida no vuelve a consultar, como el `ChipGroup` con selección obligatoria.
- **K7. Íconos.** Los vectores de `res/drawable` se convirtieron a SVG (`Resources/Images`), uno por color usado, porque MAUI no tiñe imágenes sin bibliotecas extra.
- **K8. Identificador de la app.** Es `ec.edu.monster.countryapi.dotnet`, distinto del cliente Java (`ec.edu.monster.countryapi`), para poder tener las dos apps instaladas en el mismo emulador.
- **K9. Estado al recrear la actividad.** Java guardaba la pantalla actual en `onSaveInstanceState`. Aquí la actividad declara los mismos `ConfigurationChanges` (girar, teclado, tema), así que no se recrea y no hace falta guardar el estado.
- **K10. Pruebas.**
  - Las 65 de Java, traducidas a xUnit en `pruebas/` (`net10.0`, sin emulador): las 49 del escritorio más `ConfigServidorTest`, que tiene 7 métodos y 16 casos.
  - El proyecto de pruebas enlaza los `.cs` sin MAUI, en lugar de referenciar el proyecto Android.
- **K11. Revisión en el emulador** (Pixel 7, API 36), contra el servidor .NET.
  - Recorridos: inicio de sesión con error y correcto; menú; Países (lista, Editar, formulario, validación «Ingrese el nombre del país», Cancelar); Buscar y filtrar (Europa → 2 países); Reportes; Universidades; Importar (confirmación cancelada); Consultar en la API («ecuador» con REST Countries real y Guardar); sesión expirada al reiniciar el servidor; Cerrar sesión; «Cambiar servidor» con una dirección inválida.
  - Si se maneja el emulador con teclas (`adb shell input keyevent`), Android sale del modo táctil y dibuja su resaltado de foco gris sobre la vista enfocada. Es normal de Android y desaparece al tocar la pantalla.
