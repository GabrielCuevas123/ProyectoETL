namespace ProyectoETL.Data.Services;

public static class LogService
{
    public static string GuardarRechazados(List<ResultadoCarga> resultados, string carpetaLogs)
    {
        Directory.CreateDirectory(carpetaLogs);

        var ruta = Path.Combine(carpetaLogs, $"rechazados_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

        using var escritor = new StreamWriter(ruta, false, System.Text.Encoding.UTF8);
        escritor.WriteLine($"LOG DE REGISTROS RECHAZADOS - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        escritor.WriteLine(new string('=', 70));

        foreach (var r in resultados)
        {
            escritor.WriteLine();
            escritor.WriteLine($"{r.Tabla}: procesados {r.Procesados}, insertados {r.Insertados}, rechazados {r.CantidadRechazados}");

            foreach (var motivo in r.Rechazados)
                escritor.WriteLine("  - " + motivo);
        }

        return ruta;
    }
}