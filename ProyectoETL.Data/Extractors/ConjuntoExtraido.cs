namespace ProyectoETL.Data.Extractors;

public class ConjuntoExtraido
{
    public string Nombre { get; }
    public IReadOnlyList<object> Filas { get; }

    public ConjuntoExtraido(string nombre, IReadOnlyList<object> filas)
    {
        Nombre = nombre;
        Filas = filas;
    }
}