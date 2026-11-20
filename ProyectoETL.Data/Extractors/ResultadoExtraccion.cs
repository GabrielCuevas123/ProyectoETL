namespace ProyectoETL.Data.Extractors;

public class ResultadoExtraccion
{
    public string Fuente { get; }
    public List<ConjuntoExtraido> Conjuntos { get; } = new();
    public TimeSpan Duracion { get; set; }
    public int TotalFilas => Conjuntos.Sum(c => c.Filas.Count);

    public ResultadoExtraccion(string fuente)
    {
        Fuente = fuente;
    }
}