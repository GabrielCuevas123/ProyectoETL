using ProyectoETL.Data.Cargas;
using ProyectoETL.Data.Extractors;
using ProyectoETL.Worker;

var builder = Host.CreateApplicationBuilder(args);
var config = builder.Configuration;

var claveApi = config["ApiKey"]
    ?? throw new InvalidOperationException("Falta la configuración ApiKey (secretos de usuario).");

builder.Services.AddHttpClient("ComentariosApi", cliente =>
{
    cliente.BaseAddress = new Uri(config["Extraccion:ApiUrl"]!);
    cliente.DefaultRequestHeaders.Add("X-Api-Key", claveApi);
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

// Cada fuente se registra con una línea. Para agregar otra, se crea su clase IExtractor y se registra aquí.
builder.Services.AddSingleton<IExtractor>(sp => new CsvExtractor(
    config["Extraccion:CarpetaCsv"]!,
    config["Extraccion:ArchivoEncuestas"]!,
    sp.GetRequiredService<ILogger<CsvExtractor>>()));

builder.Services.AddSingleton<IExtractor>(sp => new DatabaseExtractor(
    config.GetConnectionString("Electivadb")!,
    sp.GetRequiredService<ILogger<DatabaseExtractor>>()));

builder.Services.AddSingleton<IExtractor>(sp => new ApiExtractor(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("ComentariosApi"),
    config["Extraccion:RutaComentarios"]!,
    sp.GetRequiredService<ILogger<ApiExtractor>>()));

builder.Services.AddSingleton(sp => new StagingWriter(
    config["Extraccion:CarpetaStaging"]!,
    sp.GetRequiredService<ILogger<StagingWriter>>()));

// Carga de dimensiones (ADO.NET, capa de datos)
builder.Services.AddSingleton(sp => new StagingReader(
    config["Extraccion:CarpetaStaging"]!));

builder.Services.AddSingleton(sp => new DimensionesLoader(
    config.GetConnectionString("DWOpiniones")!,
    sp.GetRequiredService<StagingReader>(),
    sp.GetRequiredService<ILogger<DimensionesLoader>>()));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();