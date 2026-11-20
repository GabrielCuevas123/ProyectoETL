using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ProyectoETL.Data.Extractors;

public class StagingWriter
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _carpetaBase;
    private readonly ILogger<StagingWriter> _logger;

    public StagingWriter(string carpetaBase, ILogger<StagingWriter> logger)
    {
        _carpetaBase = carpetaBase;
        _logger = logger;
    }

    public async Task<string> GuardarAsync(
        IEnumerable<ResultadoExtraccion> resultados, CancellationToken cancellationToken)
    {
        var carpetaEjecucion = Path.Combine(_carpetaBase, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(carpetaEjecucion);

        foreach (var resultado in resultados)
        {
            foreach (var conjunto in resultado.Conjuntos)
            {
                var ruta = Path.Combine(carpetaEjecucion, $"{resultado.Fuente}_{conjunto.Nombre}.json");

                await using var flujo = File.Create(ruta);
                await JsonSerializer.SerializeAsync(flujo, conjunto.Filas, Opciones, cancellationToken);

                _logger.LogInformation("[Staging] {Archivo}: {Filas} filas",
                    Path.GetFileName(ruta), conjunto.Filas.Count);
            }
        }

        return carpetaEjecucion;
    }
}