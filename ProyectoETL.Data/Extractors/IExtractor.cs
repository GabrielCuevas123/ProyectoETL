namespace ProyectoETL.Data.Extractors;

public interface IExtractor
{
    string Nombre { get; }
    Task<ResultadoExtraccion> ExtraerAsync(CancellationToken cancellationToken);
}