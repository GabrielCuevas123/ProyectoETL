using ProyectoETL.Data.Cargas;
using System.Diagnostics;
using ProyectoETL.Data.Extractors;

namespace ProyectoETL.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IEnumerable<IExtractor> _extractores;
    private readonly StagingWriter _escritor;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly DimensionesLoader _cargador;

    public Worker(ILogger<Worker> logger, IEnumerable<IExtractor> extractores,
                StagingWriter escritor, DimensionesLoader cargador,
                IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _extractores = extractores;
        _escritor = escritor;
        _cargador = cargador;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reloj = Stopwatch.StartNew();

        try
        {
            var lista = _extractores.ToList();
            _logger.LogInformation("Inicio de la extracción con {Fuentes} fuentes en paralelo", lista.Count);

            // Las tres extracciones arrancan a la vez y se espera a que todas terminen.
            var intentos = await Task.WhenAll(lista.Select(e => ExtraerSeguroAsync(e, stoppingToken)));

            var msExtraccion = reloj.ElapsedMilliseconds;

            var exitosos = intentos.Where(r => r != null).Select(r => r!).ToList();
            var fallidas = lista.Count - exitosos.Count;

            if (exitosos.Count > 0)
            {
                var carpeta = await _escritor.GuardarAsync(exitosos, stoppingToken);
                _logger.LogInformation("Archivos guardados en {Carpeta}", carpeta);
            }

            reloj.Stop();

            foreach (var r in exitosos)
                _logger.LogInformation("Resumen: {Fuente} = {Filas} filas ({Ms} ms)",
                    r.Fuente, r.TotalFilas, r.Duracion.TotalMilliseconds);

            _logger.LogInformation("Extracción terminada: {Ok} de {Total} fuentes, {Filas} filas.",
                 exitosos.Count, lista.Count, exitosos.Sum(r => r.TotalFilas));

            _logger.LogInformation(
                "Tiempos: extracción en paralelo {Paralelo} ms; suma de los tiempos individuales (lo que tardaría en secuencia) {Secuencial} ms; guardado en staging {Guardado} ms; total {Total_ms} ms",
                msExtraccion, (long)exitosos.Sum(r => r.Duracion.TotalMilliseconds),
                reloj.ElapsedMilliseconds - msExtraccion, reloj.ElapsedMilliseconds);

            if (fallidas > 0)
                _logger.LogWarning("{Fallidas} fuente(s) fallaron: revisa los errores anteriores", fallidas);

            // Carga de las dimensiones al DataWarehouse
            if (exitosos.Count > 0)
            {
                try
                {
                    var relojCarga = Stopwatch.StartNew();
                    var mensajes = await _cargador.CargarAsync(stoppingToken);

                    foreach (var m in mensajes)
                        _logger.LogInformation("Carga: {Mensaje}", m);

                    _logger.LogInformation("Dimensiones cargadas en {Ms} ms", relojCarga.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "La carga de dimensiones falló; no se modificó el DataWarehouse");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La extracción se detuvo por un error inesperado");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    // Una fuente que falla no tumba a las demás: se registra el error y se devuelve null.
    private async Task<ResultadoExtraccion?> ExtraerSeguroAsync(IExtractor extractor, CancellationToken ct)
    {
        try
        {
            return await extractor.ExtraerAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La fuente {Fuente} falló", extractor.Nombre);
            return null;
        }
    }
}