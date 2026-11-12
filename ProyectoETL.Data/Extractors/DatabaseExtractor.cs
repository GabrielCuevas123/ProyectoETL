using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ProyectoETL.Data.Extractors;

public class DatabaseExtractor : IExtractor
{
    private readonly string _cadenaConexion;
    private readonly ILogger<DatabaseExtractor> _logger;

    // Para agregar otra extracción basta con agregar una línea a esta lista.
    private static readonly (string Nombre, string Sql)[] Consultas =
    {
        ("ResenasWeb",
         "SELECT IdReview, IdCliente, IdProducto, Fecha, Comentario, Rating FROM dbo.WebReviews"),

        ("Clientes",
         "SELECT IdCliente, Nombre, Email FROM dbo.Cliente"),

        ("Productos",
         @"SELECT p.IdProducto, p.Nombre, p.IdCategoria, c.NombreCategoria
           FROM dbo.Producto p
           JOIN dbo.Categoria c ON c.IdCategoria = p.IdCategoria"),

        ("Fuentes",
         @"SELECT f.IdFuente, f.FechaCarga, f.IdTipodeFuente, t.NombreFuente
           FROM dbo.FuentedeDatos f
           JOIN dbo.TipodeFuente t ON t.IdTipodeFuente = f.IdTipodeFuente"),

        ("Clasificaciones",
         "SELECT IdClasificacion, NombreClasificacion FROM dbo.Clasificacion")
    };

    public DatabaseExtractor(string cadenaConexion, ILogger<DatabaseExtractor> logger)
    {
        _cadenaConexion = cadenaConexion;
        _logger = logger;
    }

    public string Nombre => "BaseDeDatos";

    public async Task<ResultadoExtraccion> ExtraerAsync(CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        var resultado = new ResultadoExtraccion(Nombre);

        await using var conexion = new SqlConnection(_cadenaConexion);
        await conexion.OpenAsync(cancellationToken);
        _logger.LogInformation("[{Fuente}] Conexión abierta a la base {Base}", Nombre, conexion.Database);

        foreach (var (nombre, sql) in Consultas)
            resultado.Conjuntos.Add(await LeerConsultaAsync(conexion, nombre, sql, cancellationToken));

        reloj.Stop();
        resultado.Duracion = reloj.Elapsed;
        _logger.LogInformation("[{Fuente}] Extraídas {Filas} filas en {Ms} ms",
            Nombre, resultado.TotalFilas, resultado.Duracion.TotalMilliseconds);

        return resultado;
    }

    // Una sola rutina que sirve para cualquier consulta: no se repite por tabla.
    private async Task<ConjuntoExtraido> LeerConsultaAsync(
        SqlConnection conexion, string nombre, string sql, CancellationToken cancellationToken)
    {
        var filas = new List<object>();

        await using var comando = new SqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

        while (await lector.ReadAsync(cancellationToken))
        {
            var fila = new Dictionary<string, object?>();
            for (int i = 0; i < lector.FieldCount; i++)
                fila[lector.GetName(i)] = lector.IsDBNull(i) ? null : lector.GetValue(i);

            filas.Add(fila);
        }

        _logger.LogInformation("[{Fuente}] {Conjunto}: {Filas} filas", Nombre, nombre, filas.Count);
        return new ConjuntoExtraido(nombre, filas);
    }
}