using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ProyectoETL.Data.Cargas;

public class DimensionesLoader
{
    private readonly string _cadena;
    private readonly StagingReader _lector;
    private readonly ILogger<DimensionesLoader> _logger;

    public DimensionesLoader(string cadenaConexion, StagingReader lector, ILogger<DimensionesLoader> logger)
    {
        _cadena = cadenaConexion;
        _lector = lector;
        _logger = logger;
    }

    // Carga las dimensiones de la última extracción dentro de UNA sola transacción:
    // o entran todas, o no entra ninguna.
    public async Task<List<string>> CargarAsync(CancellationToken ct)
    {
        var carpeta = _lector.BuscarUltimaCarpeta();
        _logger.LogInformation("Cargando dimensiones desde {Carpeta}", carpeta);

        var mensajes = new List<string>();

        await using var cn = new SqlConnection(_cadena);
        await cn.OpenAsync(ct);
        using var tx = cn.BeginTransaction();

        try
        {
            var clasificaciones = _lector.LeerTabla(carpeta, "BaseDeDatos_Clasificaciones.json",
                ("IdClasificacion", typeof(int)), ("NombreClasificacion", typeof(string)));

            mensajes.Add(await EjecutarAsync(cn, tx, "Carga.usp_CargarDimClasificacion",
                "@p_clasificaciones", "Carga.TT_Clasificacion", clasificaciones, ct));

            var clientes = _lector.LeerTabla(carpeta, "BaseDeDatos_Clientes.json",
                ("IdCliente", typeof(int)), ("Nombre", typeof(string)), ("Email", typeof(string)));

            mensajes.Add(await EjecutarAsync(cn, tx, "Carga.usp_CargarDimCliente",
                "@p_clientes", "Carga.TT_Cliente", clientes, ct));

            var productos = _lector.LeerTabla(carpeta, "BaseDeDatos_Productos.json",
                ("IdProducto", typeof(int)), ("Nombre", typeof(string)),
                ("IdCategoria", typeof(int)), ("NombreCategoria", typeof(string)));

            mensajes.Add(await EjecutarAsync(cn, tx, "Carga.usp_CargarDimProducto",
                "@p_productos", "Carga.TT_Producto", productos, ct));

            var fuentes = _lector.LeerTabla(carpeta, "BaseDeDatos_Fuentes.json",
                ("IdFuente", typeof(int)), ("FechaCarga", typeof(DateTime)),
                ("IdTipodeFuente", typeof(int)), ("NombreFuente", typeof(string)));

            mensajes.Add(await EjecutarAsync(cn, tx, "Carga.usp_CargarDimFuente",
                "@p_fuentes", "Carga.TT_Fuente", fuentes, ct));

            tx.Commit();
        }
        catch
        {
            tx.Rollback();   // si algo falla, no queda nada a medias
            throw;
        }

        return mensajes;
    }

    // Llama a un procedimiento pasando un DataTable como parámetro de tabla (TVP).
    private async Task<string> EjecutarAsync(SqlConnection cn, SqlTransaction tx, string procedimiento,
        string parametro, string tipoDeTabla, DataTable datos, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(procedimiento, cn, tx)
        {
            CommandType = CommandType.StoredProcedure
        };

        var tvp = cmd.Parameters.Add(parametro, SqlDbType.Structured);
        tvp.TypeName = tipoDeTabla;      // nombre del tipo creado en SQL Server
        tvp.Value = datos;               // las filas viajan en una sola llamada

        var resultado = cmd.Parameters.Add("@p_result", SqlDbType.VarChar, -1);
        resultado.Direction = ParameterDirection.Output;

        var reloj = Stopwatch.StartNew();
        await cmd.ExecuteNonQueryAsync(ct);

        var mensaje = resultado.Value as string ?? "";
        _logger.LogInformation("{Procedimiento}: {Filas} filas enviadas. {Mensaje} ({Ms} ms)",
            procedimiento, datos.Rows.Count, mensaje, reloj.ElapsedMilliseconds);

        return mensaje;
    }
}