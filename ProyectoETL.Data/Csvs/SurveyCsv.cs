using CsvHelper.Configuration.Attributes;

namespace ProyectoETL.Data.Csvs;

public class SurveyCsv
{
    public string IdOpinion { get; set; } = string.Empty;
    public string IdCliente { get; set; } = string.Empty;
    public string IdProducto { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;

    [Name("Clasificación")]
    public string Clasificacion { get; set; } = string.Empty;

    [Name("PuntajeSatisfacción")]
    public string PuntajeSatisfaccion { get; set; } = string.Empty;

    public string Fuente { get; set; } = string.Empty;
}