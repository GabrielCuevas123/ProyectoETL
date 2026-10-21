using CsvHelper.Configuration.Attributes;

namespace ProyectoETL.Data.Csvs;

public class ProductoCsv
{
    public string IdProducto { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    [Name("Categoría")]
    public string Categoria { get; set; } = string.Empty;
}