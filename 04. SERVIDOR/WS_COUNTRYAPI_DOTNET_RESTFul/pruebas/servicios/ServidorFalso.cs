using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ec.edu.monster.servicios;

/// <summary>
/// Servidor HTTP local para probar el consumo de las APIs sin internet (equivale al HttpServer de
/// las pruebas Java). Escucha en 127.0.0.1 en un puerto libre, guarda las rutas pedidas y el último
/// encabezado Authorization.
/// </summary>
internal sealed class ServidorFalso : IDisposable
{
    private readonly TcpListener escucha;
    private readonly CancellationTokenSource cancelar = new();
    private readonly ConcurrentQueue<string> rutas = new();
    private volatile int codigo = 200;
    /// <summary>Cuerpo según la ruta pedida (ruta con su query).</summary>
    private volatile Func<string, string> cuerpoSegunRuta = _ => "[]";
    private volatile string? ubicacion;
    private volatile string? ultimoAuthorization;

    public ServidorFalso()
    {
        escucha = new TcpListener(IPAddress.Loopback, 0);
        escucha.Start();
        _ = Task.Run(Atender);
    }

    public string Url() => "http://127.0.0.1:" + ((IPEndPoint)escucha.LocalEndpoint).Port;

    /// <summary>Responde siempre el mismo código y cuerpo.</summary>
    public void Responder(int codigoHttp, string cuerpo)
    {
        codigo = codigoHttp;
        cuerpoSegunRuta = _ => cuerpo;
    }

    /// <summary>Responde 200 con un cuerpo que depende de la ruta (por ejemplo, según el offset).</summary>
    public void ResponderSegun(Func<string, string> segunRuta)
    {
        codigo = 200;
        cuerpoSegunRuta = segunRuta;
    }

    /// <summary>La próxima petición recibe un 301 hacia esa ubicación; las siguientes, la respuesta normal.</summary>
    public void RedirigirUnaVez(string destino) => ubicacion = destino;

    public string? UltimaRuta() => rutas.LastOrDefault();

    public List<string> Rutas() => rutas.ToList();

    public string? UltimoAuthorization() => ultimoAuthorization;

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
            _ = Task.Run(() => Responder(cliente));
        }
    }

    private async Task Responder(TcpClient cliente)
    {
        using (cliente)
        {
            NetworkStream red = cliente.GetStream();
            string encabezados = await LeerEncabezados(red);
            string[] lineas = encabezados.Split("\r\n");
            string ruta = lineas[0].Split(' ')[1];
            rutas.Enqueue(ruta);
            ultimoAuthorization = lineas
                .Skip(1)
                .Where(l => l.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase))
                .Select(l => l["Authorization:".Length..].Trim())
                .LastOrDefault();

            string respuesta;
            string? destino = ubicacion;
            if (destino != null)
            {
                // Primera petición: redirección 301 sin cuerpo.
                ubicacion = null;
                respuesta = "HTTP/1.1 301 Moved Permanently\r\nLocation: " + destino
                    + "\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";
                await Escribir(red, Encoding.ASCII.GetBytes(respuesta));
                return;
            }
            byte[] cuerpo = Encoding.UTF8.GetBytes(cuerpoSegunRuta(ruta));
            respuesta = "HTTP/1.1 " + codigo + " Respuesta\r\nContent-Type: application/json; charset=utf-8\r\n"
                + "Content-Length: " + cuerpo.Length + "\r\nConnection: close\r\n\r\n";
            await Escribir(red, [.. Encoding.ASCII.GetBytes(respuesta), .. cuerpo]);
        }
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
        return Encoding.ASCII.GetString(leidos.ToArray());
    }

    private static async Task Escribir(NetworkStream red, byte[] datos)
    {
        await red.WriteAsync(datos);
        await red.FlushAsync();
    }
}
