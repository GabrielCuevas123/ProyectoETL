using System.Data;
using System.Text.Json;

namespace ProyectoETL.Data.Cargas;

public class StagingReader
{
    private readonly string _carpetaStaging;

    public StagingReader(string carpetaStaging)
    {
        _carpetaStaging = carpetaStaging;
    }

    // Cada extracción queda en una subcarpeta con fecha y hora (20261120_133915),
    // así que la última es la que se ordena al final.
    public string BuscarUltimaCarpeta()
    {
        var ultima = Directory.GetDirectories(_carpetaStaging)
                              .OrderBy(c => c)
                              .LastOrDefault();

        return ultima ?? throw new DirectoryNotFoundException(
            $"No hay extracciones en {_carpetaStaging}. Ejecuta primero el Worker de extracción.");
    }

    // Convierte un JSON (lista de objetos) en un DataTable con las columnas indicadas, en ese orden.
    public DataTable LeerTabla(string carpeta, string archivo, params (string Nombre, Type Tipo)[] columnas)
    {
        var tabla = new DataTable();
        foreach (var (nombre, tipo) in columnas)
            tabla.Columns.Add(nombre, tipo);

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(carpeta, archivo)));

        foreach (var fila in doc.RootElement.EnumerateArray())
        {
            var nueva = tabla.NewRow();

            foreach (var (nombre, tipo) in columnas)
            {
                var valor = Buscar(fila, nombre);

                if (valor is null || valor.Value.ValueKind == JsonValueKind.Null)
                    nueva[nombre] = DBNull.Value;
                else if (tipo == typeof(int))
                    nueva[nombre] = valor.Value.GetInt32();
                else if (tipo == typeof(DateTime))
                    nueva[nombre] = valor.Value.GetDateTime();
                else
                    nueva[nombre] = valor.Value.GetString();
            }

            tabla.Rows.Add(nueva);
        }

        return tabla;
    }

    // Busca la propiedad sin importar mayúsculas o minúsculas.
    private static JsonElement? Buscar(JsonElement fila, string nombre)
    {
        foreach (var p in fila.EnumerateObject())
            if (string.Equals(p.Name, nombre, StringComparison.OrdinalIgnoreCase))
                return p.Value;
        return null;
    }
}