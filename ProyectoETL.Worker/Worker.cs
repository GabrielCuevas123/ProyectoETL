using ProyectoETL.Data.Extractors;

namespace ProyectoETL.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(ILogger<Worker> logger, ILoggerFactory loggerFactory, IConfiguration config,
                  IHttpClientFactory httpFactory, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _config = config;
        _httpFactory = httpFactory;
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
                    _loggerFactory.CreateLogger<DatabaseExtractor>()),

                new ApiExtractor(
                    _httpFactory.CreateClient("ComentariosApi"),
                    _config["Extraccion:RutaComentarios"]!,
                    _loggerFactory.CreateLogger<ApiExtractor>())
            };

            var resultados = new List<ResultadoExtraccion>();
            foreach (var extractor in extractores)
                resultados.Add(await extractor.ExtraerAsync(stoppingToken));

            var escritor = new StagingWriter(
                _config["Extraccion:CarpetaStaging"]!,
                _loggerFactory.CreateLogger<StagingWriter>());

            var carpeta = await escritor.GuardarAsync(resultados, stoppingToken);

            _logger.LogInformation("Extracción guardada en {Carpeta}. Total: {Filas} filas",
                carpeta, resultados.Sum(r => r.TotalFilas));
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