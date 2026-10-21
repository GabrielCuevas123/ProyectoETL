using Microsoft.EntityFrameworkCore;
using ProyectoETL.Data.Models;

namespace ProyectoETL.Data.Daos;

public static class ContextFactory
{

    private const string CadenaConexion = @"Data Source=DESKTOP-851L5MV\SQLEXPRESS01;Initial Catalog=Electivadb;Integrated Security=True;TrustServerCertificate=True";

    public static ElectivadbContext Crear()
    {
        var opciones = new DbContextOptionsBuilder<ElectivadbContext>()
            .UseSqlServer(CadenaConexion)
            .Options;

        return new ElectivadbContext(opciones);
    }

}