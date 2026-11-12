using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Logging;
using ProyectoETL.Data.Csvs;

namespace ProyectoETL.Data.Extractors;

public class CsvExtractor : IExtractor
{
    private readonly string _rutaArchivo;
    private readonly ILogger<CsvExtractor> _logger;

    public CsvExtractor(string carpeta, string archivo, ILogger<CsvExtractor> logger)
    {
        _rutaArchivo = Path.Combine(carpeta, archivo);
        _logger = logger;
    }

    public string Nombre => "CSV";

    public async Task<ResultadoExtraccion> ExtraerAsync(CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        var resultado = new ResultadoExtraccion(Nombre);

        if (!File.Exists(_rutaArchivo))
            throw new FileNotFoundException($"No se encontró el archivo: {_rutaArchivo}");

        _logger.LogInformation("[{Fuente}] Leyendo {Ruta}", Nombre, _rutaArchivo);

        var filas = new List<object>();
        using var lector = new StreamReader(_rutaArchivo);
        using var csv = new CsvReader(lector, CultureInfo.InvariantCulture);

        await foreach (var fila in csv.GetRecordsAsync<SurveyCsv>(cancellationToken))
            filas.Add(fila);

        resultado.Conjuntos.Add(new ConjuntoExtraido("Encuestas", filas));

        reloj.Stop();
        resultado.Duracion = reloj.Elapsed;
        _logger.LogInformation("[{Fuente}] Extraídas {Filas} filas en {Ms} ms",
            Nombre, resultado.TotalFilas, resultado.Duracion.TotalMilliseconds);

        return resultado;
    }
}