using ProyectoETL.Data.Extractors;

namespace ProyectoETL.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _config;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, ILoggerFactory loggerFactory,
                  IConfiguration config, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _config = config;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var extractores = new List<IExtractor>
            {
                new CsvExtractor(
                    _config["Extraccion:CarpetaCsv"]!,
                    _config["Extraccion:ArchivoEncuestas"]!,
                    _loggerFactory.CreateLogger<CsvExtractor>()),

                new DatabaseExtractor(
                    _config.GetConnectionString("Electivadb")!,
                    _loggerFactory.CreateLogger<DatabaseExtractor>())
            };

            foreach (var extractor in extractores)
            {
                var resultado = await extractor.ExtraerAsync(stoppingToken);

                foreach (var c in resultado.Conjuntos)
                    _logger.LogInformation("Prueba: {Fuente} / {Conjunto}: {Filas} filas",
                        resultado.Fuente, c.Nombre, c.Filas.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "La extracción falló");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}