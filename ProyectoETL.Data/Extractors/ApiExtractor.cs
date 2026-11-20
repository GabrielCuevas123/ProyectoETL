using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace ProyectoETL.Data.Extractors;

public class ApiExtractor : IExtractor
{
    private readonly HttpClient _cliente;
    private readonly string _ruta;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(HttpClient cliente, string ruta, ILogger<ApiExtractor> logger)
    {
        _cliente = cliente;
        _ruta = ruta;
        _logger = logger;
    }

    public string Nombre => "API";

    public async Task<ResultadoExtraccion> ExtraerAsync(CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        var resultado = new ResultadoExtraccion(Nombre);

        _logger.LogInformation("[{Fuente}] Llamando a {Url}", Nombre, new Uri(_cliente.BaseAddress!, _ruta));

        using var respuesta = await _cliente.GetAsync(_ruta, cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"La API respondió {(int)respuesta.StatusCode} {respuesta.ReasonPhrase}.");

        var comentarios = await respuesta.Content
            .ReadFromJsonAsync<List<ComentarioApi>>(cancellationToken) ?? new List<ComentarioApi>();

        resultado.Conjuntos.Add(new ConjuntoExtraido(
            "ComentariosRedes", comentarios.Cast<object>().ToList()));

        reloj.Stop();
        resultado.Duracion = reloj.Elapsed;
        _logger.LogInformation("[{Fuente}] Extraídos {Filas} comentarios en {Ms} ms",
            Nombre, resultado.TotalFilas, resultado.Duracion.TotalMilliseconds);

        return resultado;
    }
}

public record ComentarioApi(
    int IdComentario, int? IdCliente, int IdProducto, int IdFuente, DateOnly Fecha, string Comentario);