# MOV_COUNTRYAPI_DOTNET_RESTFul

Cliente móvil (.NET MAUI, Android) de CountryAPI. Consume **solo** el servidor RESTful `WS_COUNTRYAPI_DOTNET_RESTFul`; nunca llama a SQL Server ni a las APIs públicas.

- .NET 10 (`net10.0-android`) y .NET MAUI. Android 7.0 o superior (como el minSdk 24 del cliente Java).
- `Newtonsoft.Json` 13.0.4 para el JSON.
- Es la traducción del cliente Android de TI.2.1: mismas pantallas, textos, validaciones y mensajes.
- La interfaz sigue el diseño de referencia:
  - barra superior azul con el usuario y el estado del servidor;
  - menú de tarjetas;
  - listas con encabezado celeste;
  - avisos verdes y rojos.

## Configuración: dónde está el servidor

La dirección inicial es la propiedad `Servidor` del `.csproj`:

```xml
<Servidor Condition="'$(Servidor)' == ''">http://10.0.2.2:5088/api</Servidor>
```

- **Emulador:** `10.0.2.2` es el equipo donde corre el servidor. No hace falta cambiar nada.
- **Celular físico:** use la IP del equipo del servidor en la red local, por ejemplo `http://192.168.1.20:5088/api`. El celular y el equipo deben estar en la misma red, y el firewall de Windows debe permitir el puerto 5088. El servidor ya escucha en `0.0.0.0`.

También se puede cambiar **sin recompilar**: en la pantalla de inicio de sesión, «Cambiar servidor». La nueva dirección queda guardada en la app (`Preferences`).

Para compilar con otra dirección sin editar el archivo:

```powershell
dotnet build C:\climovcountry\MOV_COUNTRYAPI_DOTNET_RESTFul.csproj -f net10.0-android -p:Servidor=http://192.168.1.20:5088/api
```

Cada petición espera como máximo 15 segundos (`ConfigServidor.TIMEOUT_SEGUNDOS`).

## Compilar e instalar

Requisitos:

- el servidor en marcha (ver `04. SERVIDOR/WS_COUNTRYAPI_DOTNET_RESTFul/README.md`);
- la base levantada con `03. BDD/docker-compose.yml`;
- el workload de Android de .NET y un emulador o celular conectado.

La ruta del repositorio pasa de 260 caracteres dentro de `obj/`, y `aapt2` falla. Por eso se compila desde **PowerShell** a través de un junction corto (creado una sola vez):

```powershell
New-Item -ItemType Junction -Path C:\climovcountry -Target "<ruta del repositorio>\04. CLIMOV\MOV_COUNTRYAPI_DOTNET_RESTFul"

dotnet build C:\climovcountry\MOV_COUNTRYAPI_DOTNET_RESTFul.csproj -f net10.0-android               # compila
dotnet build C:\climovcountry\MOV_COUNTRYAPI_DOTNET_RESTFul.csproj -f net10.0-android -t:Install    # instala en el emulador
```

También se abre con Visual Studio (desde `C:\climovcountry`) y se ejecuta con ▶ en el emulador.

Usuarios de prueba: `monster` / `monster9` y `admin` / `admin123`.

## Pruebas

```bash
dotnet test pruebas
dotnet test pruebas --filter "FullyQualifiedName~ControladoresTest"
```

Las 65 pruebas (las mismas de Java) son xUnit, corren en el equipo (sin emulador) y no necesitan el servidor real. El proyecto de pruebas enlaza el código de la app que no depende de MAUI (modelos, config, servicios, controladores e interfaces de vista):

- **`ValidadorDatosTest`:** las validaciones del ECUD.
- **`ServiciosTest`:** rutas, encabezados, JSON, 401, sin conexión y respuesta no válida, contra un servidor HTTP falso.
- **`ControladoresTest`:** cada controlador con una vista en memoria (`dobles/VistaFalsa`) y el servidor falso (`dobles/ServidorFalso`).
- **`ConfigServidorTest`:** direcciones válidas e inválidas del servidor y cambio de servidor sin reiniciar.

## Estructura (diagrama DC_Clientes)

```
├── App.xaml(.cs), MauiProgram.cs   Arranque de MAUI: una sola ventana con PaginaPrincipal
├── AplicacionCountryApi.cs         Crea ClienteRest, la sesión y los servicios una sola vez
├── config/          ConfigServidor (dirección de compilación o la elegida en la app, TIMEOUT_SEGUNDOS = 15)
├── modelos/         Pais, Reporte, Universidad, RespuestaDTO<T>, ...     (iguales al escritorio)
├── servicios/       ClienteRest (HttpClient + Newtonsoft.Json), SesionServicio y un servicio por módulo
├── controlador/     Login, Pais, Api, Reporte, Universidad y ValidadorDatos (iguales al escritorio)
├── vista/           Interfaces del diagrama (VistaBase, LoginVista, PaisVista, ...) y su
│                    implementación MAUI: PaginaPrincipal (MenuVista) y una pantalla por módulo
├── Platforms/Android, Resources/   Actividad, manifiesto, íconos, estilos (Resources/Styles/Estilos.xaml)
└── pruebas/         Proyecto xUnit aparte (excluido del .csproj principal), con dobles/
```

Las dependencias van en un solo sentido: `vista → controlador → servicios → ClienteRest`.

- Los controladores son los mismos del cliente de escritorio y solo conocen las interfaces de vista.
- Cada acción corre fuera del hilo principal. Mientras tanto, aparece la barra de progreso y la pantalla no acepta toques.
- Las pantallas cambian sus controles en el hilo principal (`Hilos`). Los diálogos, como el formulario o las confirmaciones, esperan la respuesta del usuario.

## Funciones

- **Inicio y cierre de sesión.**
  - Si el servidor rechaza el token (sesión expirada o servidor reiniciado), la app vuelve al inicio de sesión con el aviso.
  - «Cerrar sesión» está en la barra superior y en el menú.
- **Importar países:** pide confirmación y muestra el resumen (recibidos, registrados y actualizados).
- **Consultar un país en la API:** al menos 2 caracteres. Cada resultado indica si ya está guardado y tiene su botón «Guardar».
- **Países:**
  - búsqueda por nombre y botón «Nuevo»;
  - al tocar un país, «Editar» o «Eliminar», que pide confirmación;
  - el formulario se reabre con lo escrito y el mensaje si los datos no son válidos.
- **Buscar y filtrar:** nombre y capital parciales. Los filtros de región («Todas», «América», ...) vuelven a consultar al elegirlos.
- **Reportes:** país más poblado, países por región y promedio poblacional por región.
- **Universidades:** importar las de un país (en inglés) y ver las guardadas.
- **Formato:** población y área se muestran con separador de miles (`17.643.060`).
- **Navegación:** la flecha de la barra o «atrás» vuelve al menú; con un diálogo abierto, «atrás» lo cancela. En el menú, «atrás» deja la app en segundo plano sin cerrar la sesión.
