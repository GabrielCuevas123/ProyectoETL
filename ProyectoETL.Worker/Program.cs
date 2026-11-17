using ProyectoETL.Worker;

var builder = Host.CreateApplicationBuilder(args);

var claveApi = builder.Configuration["ApiKey"]
    ?? throw new InvalidOperationException("Falta la configuración ApiKey (secretos de usuario).");

builder.Services.AddHttpClient("ComentariosApi", cliente =>
{
    cliente.BaseAddress = new Uri(builder.Configuration["Extraccion:ApiUrl"]!);
    cliente.DefaultRequestHeaders.Add("X-Api-Key", claveApi);
    cliente.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();