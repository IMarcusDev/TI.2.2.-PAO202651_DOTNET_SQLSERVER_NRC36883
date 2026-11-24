# ESC_COUNTRYAPI_DOTNET_RESTFul

Cliente de escritorio (WPF) de CountryAPI. Consume **solo** el servidor RESTful `WS_COUNTRYAPI_DOTNET_RESTFul`; nunca llama a SQL Server ni a las APIs públicas.

- .NET 10 (`net10.0-windows`) y WPF. `Newtonsoft.Json` 13.0.4 para el JSON.
- Es la traducción del cliente Swing de TI.2.1: mismas pantallas, textos, validaciones y mensajes.
- La interfaz sigue el diseño de referencia:
  - barra superior azul con la navegación y el usuario;
  - menú de tarjetas;
  - tablas con encabezado celeste;
  - alertas verdes y rojas;
  - indicador de si el servidor responde.

## Configuración: dónde está el servidor

La dirección está en `appsettings.json`:

```json
{
  "Servidor": {
    "Url": "http://localhost:5088/api"
  }
}
```

Si el servidor está en otra PC, cambie solo la IP, por ejemplo `http://192.168.1.20:5088/api`.

El archivo se busca en este orden:

1. la ruta indicada en la variable de entorno `COUNTRYAPI_CONFIG`;
2. `appsettings.json` en la carpeta desde donde se ejecuta;
3. `appsettings.json` junto al `.exe`. Al compilar se copia a `bin/`, y ahí se puede editar sin recompilar.

Si no encuentra ninguno, usa `http://localhost:5088/api`. Cada petición espera como máximo 15 segundos (`ConfigServidor.TIMEOUT_SEGUNDOS`).

## Compilar y ejecutar

Requisitos:

- el servidor en marcha (ver `04. SERVIDOR/WS_COUNTRYAPI_DOTNET_RESTFul/README.md`);
- la base levantada con `03. BDD/docker-compose.yml`.

```bash
dotnet build
dotnet run
```

También se puede abrir `ESC_COUNTRYAPI_DOTNET_RESTFul.csproj` en Visual Studio y ejecutarlo con F5.

Usuarios de prueba: `monster` / `monster9` y `admin` / `admin123`.

## Pruebas

```bash
dotnet test pruebas
dotnet test pruebas --filter "FullyQualifiedName~ControladoresTest"     # una clase
dotnet test pruebas --filter "FullyQualifiedName~ValidadorDatosTest"
```

Las 49 pruebas (las mismas de Java) no necesitan el servidor real:

- **`ValidadorDatosTest`:** las validaciones del ECUD.
- **`ServiciosTest`:** rutas, encabezados, JSON, 401, sin conexión y respuesta no válida, contra un servidor HTTP falso.
- **`ControladoresTest`:** cada controlador con una vista en memoria (`dobles/VistaFalsa`) y el servidor falso (`dobles/ServidorFalso`, un `TcpListener` en un puerto libre).

## Estructura (diagrama DC_Clientes)

```
├── App.xaml(.cs)     Punto de entrada: crea servicios, vistas y controladores y los conecta
├── appsettings.json  Servidor:Url
├── config/           ConfigServidor (URL_BASE desde appsettings.json, TIMEOUT_SEGUNDOS = 15)
├── modelos/          Pais, Reporte, Universidad, RespuestaDTO<T>, ...
├── servicios/        ClienteRest (HttpClient + Newtonsoft.Json), SesionServicio y un servicio por módulo
├── controlador/      Login, Pais, Api, Reporte, Universidad y ValidadorDatos
├── vista/            Interfaces del diagrama (VistaBase, LoginVista, PaisVista, ...) y su
│                     implementación WPF (LoginPanel, PaisPanel, ..., VentanaPrincipal, Estilos.xaml)
└── pruebas/          Proyecto xUnit aparte (excluido del .csproj principal), con dobles/
```

Las dependencias van en un solo sentido: `vista → controlador → servicios → ClienteRest`. Los controladores solo conocen las interfaces de vista; por eso se prueban sin pantalla.

## Funciones

- **Inicio y cierre de sesión.** Si el servidor rechaza el token (sesión expirada o servidor reiniciado), la aplicación vuelve al inicio de sesión con el aviso.
- **Importar países:** pide confirmación y muestra el resumen (recibidos, registrados y actualizados).
- **Consultar un país en la API:** al menos 2 caracteres. Se ve si cada país ya está guardado y se puede guardar el elegido.
- **Países:** `DataGrid` con búsqueda por nombre y botones Nuevo, Editar y Eliminar.
  - Eliminar pide confirmación.
  - El formulario valida nombre, región, población y área y, si hay error, se reabre con lo escrito.
  - Atajos: Alt+N, Alt+E y Alt+L; doble clic en una fila para editar.
- **Buscar y filtrar:** nombre y capital parciales. Los filtros de región («Todas», «América», ...) vuelven a consultar al elegirlos.
- **Reportes:** países por región, país más poblado y promedio poblacional por región.
- **Universidades:** importar las de un país (en inglés) y ver las guardadas.
- **Mientras se procesa una petición:**
  - aparece la barra de progreso y la ventana no acepta clics;
  - la llamada es asíncrona (`async`/`await`), así que la interfaz no se congela;
  - un error inesperado se muestra en pantalla y no cierra la aplicación.
- **Formato:** población y área se muestran con separador de miles (`17.643.060`).
