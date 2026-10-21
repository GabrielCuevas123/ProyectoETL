using ProyectoETL.Data.Daos;
using ProyectoETL.Data.Services;
using ProyectoETL.Data.Models;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var carpeta = @"C:\DataOpiniones";

try
{
    using var db = ContextFactory.Crear();
    var servicio = new CargaService(db);



    bool limpiarAntes = false;   // TRUE: VACÍA LAS TABLAS ANTES DE CARGAR. VUELVE A PONERLO EN FALSE DESPUÉS.

    if (limpiarAntes)
    {
        var salidaLimpieza = new OutputParameter<string>();
        await db.GetProcedures().usp_LimpiarDatosAsync(salidaLimpieza);
        Console.WriteLine($"Limpieza de tablas (usp_LimpiarDatos): {salidaLimpieza.Value}");
        Console.WriteLine();
    }


    var resultados = new List<ResultadoCarga>
    {
        // GRUPO 1: CATÁLOGOS
        servicio.CargarCategorias(carpeta),
        servicio.CargarTiposDeFuente(carpeta),
        servicio.CargarClasificaciones(carpeta),

        // GRUPO 2: MAESTROS
        servicio.CargarClientes(carpeta),
        servicio.CargarProductos(carpeta),
        servicio.CargarFuentesDeDatos(carpeta),

        // GRUPO 3: OPINIONES
        servicio.CargarWebReviews(carpeta),
        servicio.CargarSocialComments(carpeta),
        servicio.CargarSurveys(carpeta)
    };

    Console.WriteLine("RESUMEN DEL PROCESO ETL");
    Console.WriteLine(new string('-', 50));
    Console.WriteLine($"{"Tabla",-16}{"Procesados",11}{"Insertados",12}{"Rechazados",12}");
    Console.WriteLine(new string('-', 50));

    foreach (var r in resultados)
        Console.WriteLine($"{r.Tabla,-16}{r.Procesados,11}{r.Insertados,12}{r.CantidadRechazados,12}");

    Console.WriteLine(new string('-', 50));
    Console.WriteLine($"{"TOTAL",-16}{resultados.Sum(r => r.Procesados),11}{resultados.Sum(r => r.Insertados),12}{resultados.Sum(r => r.CantidadRechazados),12}");
   
    var rutaLog = LogService.GuardarRechazados(resultados, Path.Combine(carpeta, "Logs"));
    Console.WriteLine($"Log de rechazados: {rutaLog}");


    var procedimientos = db.GetProcedures();
    var salida = new OutputParameter<string>();
    var conteos = await procedimientos.usp_ConteoTablasAsync(salida);

    Console.WriteLine();
    Console.WriteLine($"CONTEOS REALES EN LA BASE (usp_ConteoTablas: {salida.Value})");
    foreach (var c in conteos)
        Console.WriteLine($"{c.Tabla,-16}{c.Registros,11}");
}
catch (Exception ex)
{
    Console.WriteLine($"El proceso se detuvo por un error: {ex.Message}");
}