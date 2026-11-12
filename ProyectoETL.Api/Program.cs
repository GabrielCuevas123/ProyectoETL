using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var cadenaConexion = app.Configuration.GetConnectionString("Electivadb")!;

app.MapGet("/api/comentarios", async (CancellationToken ct) =>
{
    var comentarios = new List<ComentarioDto>();

    await using var conexion = new SqlConnection(cadenaConexion);
    await conexion.OpenAsync(ct);

    const string sql =
        "SELECT IdComentario, IdCliente, IdProducto, IdFuente, Fecha, Comentario " +
        "FROM dbo.SocialComments ORDER BY IdComentario";

    await using var comando = new SqlCommand(sql, conexion);
    await using var lector = await comando.ExecuteReaderAsync(ct);

    while (await lector.ReadAsync(ct))
    {
        comentarios.Add(new ComentarioDto(
            lector.GetInt32(0),
            lector.IsDBNull(1) ? null : lector.GetInt32(1),
            lector.GetInt32(2),
            lector.GetInt32(3),
            DateOnly.FromDateTime(lector.GetDateTime(4)),
            lector.GetString(5)));
    }

    return Results.Ok(comentarios);
});

app.Run();

public record ComentarioDto(
    int IdComentario, int? IdCliente, int IdProducto, int IdFuente, DateOnly Fecha, string Comentario);