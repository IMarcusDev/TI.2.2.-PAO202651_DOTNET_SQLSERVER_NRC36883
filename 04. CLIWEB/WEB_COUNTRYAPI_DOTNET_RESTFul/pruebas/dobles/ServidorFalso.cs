using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ec.edu.monster.dobles;

/// <summary>
/// Servidor RESTful falso para probar servicios y controladores sin el servidor real ni la base.
/// Es un servidor HTTP de verdad (TcpListener en 127.0.0.1, puerto libre), como el HttpServer de
/// las pruebas Java.
///
/// Cada respuesta se registra con «MÉTODO /ruta» (la ruta sin la query, desde /api). Las
/// peticiones recibidas quedan guardadas para comprobar qué envió el cliente.
/// </summary>
public sealed class ServidorFalso : IDisposable
{
    /// <summary>Petición recibida: método, ruta con query, encabezado Authorization y cuerpo.</summary>
    public sealed record Peticion(string Metodo, string Ruta, string? Authorization, string Cuerpo);

    private sealed record Respuesta(int Codigo, string Cuerpo);

    private readonly TcpListener escucha;
    private readonly CancellationTokenSource cancelar = new();
    private readonly ConcurrentDictionary<string, Respuesta> respuestas = new();
    private readonly ConcurrentQueue<Peticion> peticiones = new();

    public ServidorFalso()
    {
        escucha = new TcpListener(IPAddress.Loopback, 0);
        escucha.Start();
        _ = Task.Run(Atender);
    }

    /// <summary>URL base equivalente a la del servidor real (termina en /api).</summary>
    public string UrlBase() => "http://127.0.0.1:" + ((IPEndPoint)escucha.LocalEndpoint).Port + "/api";

    /// <summary>Responde a «MÉTODO /ruta» con ese código y ese JSON.</summary>
    public void Responder(string metodoYRuta, int codigo, string json)
    {
        respuestas[metodoYRuta] = new Respuesta(codigo, json);
    }

    /// <summary>Atajo para una respuesta OK con su mensaje y sus datos (JSON).</summary>
    public void Ok(string metodoYRuta, string mensaje, string datosJson)
    {
        Responder(metodoYRuta, 200, "{\"estado\":\"OK\",\"mensaje\":\"" + mensaje + "\",\"datos\":" + datosJson + "}");
    }

    /// <summary>Atajo para una respuesta de error con su código y su mensaje.</summary>
    public void Error(string metodoYRuta, int codigo, string mensaje)
    {
        Responder(metodoYRuta, codigo, "{\"estado\":\"ERROR\",\"mensaje\":\"" + mensaje + "\",\"datos\":null}");
    }

    public List<Peticion> Peticiones() => peticiones.ToList();

    public Peticion? Ultima() => peticiones.LastOrDefault();

    public void Dispose()
    {
        cancelar.Cancel();
        escucha.Stop();
    }

    private async Task Atender()
    {
        while (!cancelar.IsCancellationRequested)
        {
            TcpClient cliente;
            try
            {
                cliente = await escucha.AcceptTcpClientAsync(cancelar.Token);
            }
            catch (Exception)
            {
                return;
            }
            _ = Task.Run(() => Contestar(cliente));
        }
    }

    private async Task Contestar(TcpClient cliente)
    {
        using (cliente)
        {
            NetworkStream red = cliente.GetStream();
            string[] lineas = (await LeerEncabezados(red)).Split("\r\n");
            string[] solicitud = lineas[0].Split(' ');
            string metodo = solicitud[0];
            string destino = solicitud[1];
            int signo = destino.IndexOf('?');
            string rutaCruda = signo < 0 ? destino : destino[..signo];
            string? query = signo < 0 ? null : destino[(signo + 1)..];

            string? authorization = Encabezado(lineas, "Authorization");
            int largo = int.TryParse(Encabezado(lineas, "Content-Length"), out int n) ? n : 0;
            string cuerpo = largo > 0 ? Encoding.UTF8.GetString(await LeerBytes(red, largo)) : "";

            string ruta = rutaCruda.StartsWith("/api") ? rutaCruda["/api".Length..] : rutaCruda;
            peticiones.Enqueue(new Peticion(metodo, query == null ? ruta : ruta + "?" + query, authorization, cuerpo));

            Respuesta r = respuestas.GetValueOrDefault(metodo + " " + ruta)
                ?? new Respuesta(404, "{\"estado\":\"ERROR\",\"mensaje\":\"Ruta no configurada en la prueba\",\"datos\":null}");
            byte[] bytes = Encoding.UTF8.GetBytes(r.Cuerpo);
            string cabecera = "HTTP/1.1 " + r.Codigo + " Respuesta\r\nContent-Type: application/json; charset=UTF-8\r\n"
                + "Content-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n";
            byte[] completa = [.. Encoding.ASCII.GetBytes(cabecera), .. bytes];
            await red.WriteAsync(completa);
            await red.FlushAsync();
        }
    }

    private static string? Encabezado(string[] lineas, string nombre)
    {
        return lineas.Skip(1)
            .Where(l => l.StartsWith(nombre + ":", StringComparison.OrdinalIgnoreCase))
            .Select(l => l[(nombre.Length + 1)..].Trim())
            .FirstOrDefault();
    }

    private static async Task<string> LeerEncabezados(NetworkStream red)
    {
        var leidos = new List<byte>();
        var buffer = new byte[1];
        while (await red.ReadAsync(buffer) == 1)
        {
            leidos.Add(buffer[0]);
            int n = leidos.Count;
            if (n >= 4 && leidos[n - 4] == '\r' && leidos[n - 3] == '\n' && leidos[n - 2] == '\r' && leidos[n - 1] == '\n')
            {
                break;
            }
        }
        return Encoding.ASCII.GetString(leidos.ToArray(), 0, Math.Max(0, leidos.Count - 4));
    }

    private static async Task<byte[]> LeerBytes(NetworkStream red, int largo)
    {
        var datos = new byte[largo];
        int leidos = 0;
        while (leidos < largo)
        {
            int n = await red.ReadAsync(datos.AsMemory(leidos, largo - leidos));
            if (n == 0)
            {
                break;
            }
            leidos += n;
        }
        return datos;
    }
}
