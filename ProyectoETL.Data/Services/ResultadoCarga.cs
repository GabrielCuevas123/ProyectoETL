namespace ProyectoETL.Data.Services
{
    public class ResultadoCarga
    {
        public string Tabla { get; }
        public int Procesados { get; set; }
        public int Insertados { get; set; }
        public List<string> Rechazados { get; } = new();
        public int CantidadRechazados => Rechazados.Count;
        public ResultadoCarga(string tabla)
        {
            Tabla = tabla;
        }

    }

}