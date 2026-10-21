using System.Globalization;
using CsvHelper;
using ProyectoETL.Data.Csvs;
using ProyectoETL.Data.Models;

namespace ProyectoETL.Data.Services;

public class CargaService
{

    // Constructor: recibe la conexión a la base de datos
    private readonly ElectivadbContext _db;
    public CargaService(ElectivadbContext db)
    {
        _db = db;
    }

    // MÉTODO DE LECTURA 
    public List<T> Obtener<T>(string ruta)
    {
        if (!File.Exists(ruta))
            throw new FileNotFoundException($"No se encontró el archivo: {ruta}");

        using var lector = new StreamReader(ruta);
        using var csv = new CsvReader(lector, CultureInfo.InvariantCulture);
        return csv.GetRecords<T>().ToList();
    }

    // MÉTODO DE LIMPIEZA 
    public List<T> Limpiar<T>(List<T> filas) where T : class
    {
        var propiedades = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite)
            .ToList();

        foreach (var fila in filas)
        {
            foreach (var propiedad in propiedades)
            {
                var valor = (string?)propiedad.GetValue(fila) ?? "";
                propiedad.SetValue(fila, valor.Trim());
            }
        }

        return filas
            .Where(fila => propiedades.Any(p => ((string?)p.GetValue(fila) ?? "") != ""))
            .ToList();
    }

    // MÉTODO DE LECTURA Y LIMPIEZA 
    public List<T> ObtenerLimpio<T>(string carpeta, string archivo) where T : class
    {
        return Limpiar(Obtener<T>(Path.Combine(carpeta, archivo)));
    }

    // AYUDANTE DE VALORES ÚNICOS 
    public List<string> ValoresUnicos<T>(List<T> filas, Func<T, string> columna)
     {
        return filas
            .Select(columna)
            .Where(valor => valor != "")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    //AYUDANTE DE BÚSQUEDA DE ID POR NOMBRE 
    public int BuscarId(Dictionary<string, int> diccionario, string valor, string columna)
    {
        if (!diccionario.TryGetValue(valor, out int id))
            throw new InvalidDataException($"{columna}: '{valor}' no existe en la base.");

        return id;
    }

    // AYUDANTE DE VERIFICACIÓN DE ID 
    public int VerificarId(HashSet<int> existentes, int valor, string columna)
    {
        if (!existentes.Contains(valor))
            throw new InvalidDataException($"{columna}: el ID {valor} no existe en la base.");

        return valor;
    }

    // MÉTODO DE CARGA
    public ResultadoCarga Cargar<TCsv, TEntidad>(
    string nombreTabla,
    List<TCsv> filas,
    Func<TCsv, TEntidad> convertir,
    Func<TEntidad, string> clave,
    IEnumerable<string> clavesExistentes) where TEntidad : class
    {
        var resultado = new ResultadoCarga(nombreTabla);

        var existentes = new HashSet<string>(clavesExistentes, StringComparer.OrdinalIgnoreCase);
        var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int numeroFila = 1;

        foreach (var fila in filas)
        {
            numeroFila++;
            resultado.Procesados++;

            try
            {
                var entidad = convertir(fila);
                var id = clave(entidad);

                if (!vistas.Add(id))
                    throw new InvalidDataException($"clave '{id}' duplicada dentro del archivo.");

                if (existentes.Contains(id))
                    throw new InvalidDataException($"clave '{id}' duplicada, ya existe en la base.");

                _db.Set<TEntidad>().Add(entidad);
                resultado.Insertados++;
            }
            catch (InvalidDataException ex)
            {
                resultado.Rechazados.Add($"{nombreTabla} fila {numeroFila}: {ex.Message}");
            }
        }

        try
        {
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            resultado.Insertados = 0;
            resultado.Rechazados.Add($"Error al guardar {nombreTabla}: {ex.Message}");
        }
        finally
        {
            _db.ChangeTracker.Clear();
        }

        return resultado;
    }

    // CARGA DE CLIENTES
    public ResultadoCarga CargarClientes(string carpeta)
    {
        var filas = ObtenerLimpio<ClienteCsv>(carpeta, "clients.csv");

        return Cargar<ClienteCsv, Cliente>(
            "Cliente",
            filas,
            fila =>
            {
                string email = Validador.Correo(fila.Email, "Email");
                return new Cliente
                {
                    IdCliente = Validador.Entero(fila.IdCliente, "IdCliente"),
                    Nombre = Validador.Texto(fila.Nombre, 60, "Nombre"),
                    Email = email == "" ? null : email
                };
            },
            c => c.IdCliente.ToString(),
            _db.Cliente.Select(c => c.IdCliente.ToString()).ToList());
    }

    // CARGA DE PRODUCTOS
    public ResultadoCarga CargarProductos(string carpeta)
    {
        var filas = ObtenerLimpio<ProductoCsv>(carpeta, "products.csv");

        var categorias = _db.Categoria.ToDictionary(
            c => c.NombreCategoria,
            c => c.IdCategoria,
            StringComparer.OrdinalIgnoreCase);

        return Cargar<ProductoCsv, Producto>(
            "Producto",
            filas,
            fila => new Producto
            {
                IdProducto = Validador.Entero(fila.IdProducto, "IdProducto"),
                Nombre = Validador.Texto(fila.Nombre, 60, "Nombre"),
                IdCategoria = BuscarId(categorias, Validador.Texto(fila.Categoria, 60, "Categoría"), "Categoría")
            },

            p => p.IdProducto.ToString(),
            _db.Producto.Select(p => p.IdProducto.ToString()).ToList());
    }

    // CARGA DE FUENTES DE DATOS
    public ResultadoCarga CargarFuentesDeDatos(string carpeta)
    {
        var filas = ObtenerLimpio<FuenteDatosCsv>(carpeta, "fuente_datos.csv");

        var tipos = _db.TipodeFuente.ToDictionary(
            t => t.NombreFuente,
            t => t.IdTipodeFuente,
            StringComparer.OrdinalIgnoreCase);

        return Cargar<FuenteDatosCsv, FuentedeDatos>(
            "FuentedeDatos",
            filas,
            fila => new FuentedeDatos
            {
                IdFuente = Validador.EnteroConPrefijo(fila.IdFuente, 'F', "IdFuente"),
                FechaCarga = Validador.Fecha(fila.FechaCarga, "FechaCarga"),
                IdTipodeFuente = BuscarId(tipos, Validador.Texto(fila.TipoFuente, 60, "TipoFuente"), "TipoFuente")
            },

            f => f.IdFuente.ToString(),
            _db.FuentedeDatos.Select(f => f.IdFuente.ToString()).ToList());
    }

    // CARGA DEL CATÁLOGO DE CATEGORÍAS
    public ResultadoCarga CargarCategorias(string carpeta)
    {
        var productos = ObtenerLimpio<ProductoCsv>(carpeta, "products.csv");

        var nombres = ValoresUnicos(productos, p => p.Categoria);

        int siguienteId = (_db.Categoria.Max(c => (int?)c.IdCategoria) ?? 0) + 1;

        return Cargar<string, Categoria>(
            "Categoria",
            nombres,
            nombre => new Categoria
            {
                IdCategoria = siguienteId++,
                NombreCategoria = Validador.Texto(nombre, 60, "NombreCategoria")
            },
            c => c.NombreCategoria,
            _db.Categoria.Select(c => c.NombreCategoria).ToList());
    }

    // CARGA DEL CATÁLOGO DE TIPOS DE FUENTE
    public ResultadoCarga CargarTiposDeFuente(string carpeta)
    {
        var fuentes = ObtenerLimpio<FuenteDatosCsv>(carpeta, "fuente_datos.csv");

        var nombres = ValoresUnicos(fuentes, f => f.TipoFuente);

        int siguienteId = (_db.TipodeFuente.Max(t => (int?)t.IdTipodeFuente) ?? 0) + 1;

        return Cargar<string, TipodeFuente>(
            "TipodeFuente",
            nombres,
            nombre => new TipodeFuente
            {
                IdTipodeFuente = siguienteId++,
                NombreFuente = Validador.Texto(nombre, 60, "NombreFuente")
            },
            t => t.NombreFuente,
            _db.TipodeFuente.Select(t => t.NombreFuente).ToList());
    }

    // CARGA DEL CATÁLOGO DE CLASIFICACIONES
    public ResultadoCarga CargarClasificaciones(string carpeta)
    {
        var encuestas = ObtenerLimpio<SurveyCsv>(carpeta, "surveys_part1.csv");

        var nombres = ValoresUnicos(encuestas, s => s.Clasificacion);

        int siguienteId = (_db.Clasificacion.Max(c => (int?)c.IdClasificacion) ?? 0) + 1;

        return Cargar<string, Clasificacion>(
            "Clasificacion",
            nombres,
            nombre => new Clasificacion
            {
                IdClasificacion = siguienteId++,
                NombreClasificacion = Validador.Texto(nombre, 60, "NombreClasificacion")
            },
            c => c.NombreClasificacion,
            _db.Clasificacion.Select(c => c.NombreClasificacion).ToList());
    }

    // CARGA DE RESEÑAS WEB
    public ResultadoCarga CargarWebReviews(string carpeta)
    {
        var filas = ObtenerLimpio<WebReviewCsv>(carpeta, "web_reviews.csv");

        var clientes = _db.Cliente.Select(c => c.IdCliente).ToHashSet();
        var productos = _db.Producto.Select(p => p.IdProducto).ToHashSet();

        return Cargar<WebReviewCsv, WebReviews>(
            "WebReviews",
            filas,
            fila => new WebReviews
            {
                IdReview = Validador.EnteroConPrefijo(fila.IdReview, 'W', "IdReview"),
                IdCliente = VerificarId(clientes, Validador.EnteroConPrefijo(fila.IdCliente, 'C', "IdCliente"), "IdCliente"),
                IdProducto = VerificarId(productos, Validador.EnteroConPrefijo(fila.IdProducto, 'P', "IdProducto"), "IdProducto"),
                Fecha = Validador.Fecha(fila.Fecha, "Fecha"),
                Comentario = Validador.Texto(fila.Comentario, 255, "Comentario"),
                Rating = Validador.EnteroEnRango(fila.Rating, 1, 5, "Rating")
            },
            r => r.IdReview.ToString(),
            _db.WebReviews.Select(r => r.IdReview.ToString()).ToList());
    }

    // CARGA DE COMENTARIOS DE REDES SOCIALES
    public ResultadoCarga CargarSocialComments(string carpeta)
    {
        var filas = ObtenerLimpio<SocialCommentCsv>(carpeta, "social_comments.csv");

        var clientes = _db.Cliente.Select(c => c.IdCliente).ToHashSet();
        var productos = _db.Producto.Select(p => p.IdProducto).ToHashSet();

        // REGLA: TODOS LOS COMENTARIOS DE REDES SOCIALES RECIBEN LA PRIMERA FUENTE DE TIPO "RED SOCIAL"
        int idFuenteRedSocial = _db.FuentedeDatos
            .Where(f => _db.TipodeFuente.Any(t => t.IdTipodeFuente == f.IdTipodeFuente && t.NombreFuente == "Red Social"))
            .OrderBy(f => f.IdFuente)
            .Select(f => (int?)f.IdFuente)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No hay ninguna fuente de tipo 'Red Social' en FuentedeDatos. Cárgala primero.");

        return Cargar<SocialCommentCsv, SocialComments>(
            "SocialComments",
            filas,
            fila => new SocialComments
            {
                IdComentario = Validador.EnteroConPrefijo(fila.IdComment, 'T', "IdComment"),
                IdCliente = fila.IdCliente == ""
                    ? null
                    : (int?)VerificarId(clientes, Validador.EnteroConPrefijo(fila.IdCliente, 'C', "IdCliente"), "IdCliente"),
                IdProducto = VerificarId(productos, Validador.EnteroConPrefijo(fila.IdProducto, 'P', "IdProducto"), "IdProducto"),
                IdFuente = idFuenteRedSocial,
                Fecha = Validador.Fecha(fila.Fecha, "Fecha"),
                Comentario = Validador.Texto(fila.Comentario, 255, "Comentario")
            },
            s => s.IdComentario.ToString(),
            _db.SocialComments.Select(s => s.IdComentario.ToString()).ToList());
    }

    // CARGA DE ENCUESTAS
    public ResultadoCarga CargarSurveys(string carpeta)
    {
        var filas = ObtenerLimpio<SurveyCsv>(carpeta, "surveys_part1.csv");

        var clientes = _db.Cliente.Select(c => c.IdCliente).ToHashSet();
        var productos = _db.Producto.Select(p => p.IdProducto).ToHashSet();

        var clasificaciones = _db.Clasificacion.ToDictionary(
            c => c.NombreClasificacion,
            c => c.IdClasificacion,
            StringComparer.OrdinalIgnoreCase);

        // REGLA: TODAS LAS ENCUESTAS RECIBEN LA PRIMERA FUENTE DE TIPO "CSV"
        int idFuenteCsv = _db.FuentedeDatos
            .Where(f => _db.TipodeFuente.Any(t => t.IdTipodeFuente == f.IdTipodeFuente && t.NombreFuente == "CSV"))
            .OrderBy(f => f.IdFuente)
            .Select(f => (int?)f.IdFuente)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No hay ninguna fuente de tipo 'CSV' en FuentedeDatos. Cárgala primero.");

        return Cargar<SurveyCsv, Surveys>(
            "Surveys",
            filas,
            fila => new Surveys
            {
                IdSurvey = Validador.Entero(fila.IdOpinion, "IdOpinion"),
                IdCliente = VerificarId(clientes, Validador.Entero(fila.IdCliente, "IdCliente"), "IdCliente"),
                IdProducto = VerificarId(productos, Validador.Entero(fila.IdProducto, "IdProducto"), "IdProducto"),
                IdFuente = idFuenteCsv,
                IdClasificacion = BuscarId(clasificaciones, Validador.Texto(fila.Clasificacion, 60, "Clasificación"), "Clasificación"),
                Fecha = Validador.Fecha(fila.Fecha, "Fecha"),
                Comentario = Validador.Texto(fila.Comentario, 255, "Comentario"),
                PuntajeClasificacion = Validador.EnteroEnRango(fila.PuntajeSatisfaccion, 1, 5, "PuntajeSatisfacción")
            },
            s => s.IdSurvey.ToString(),
            _db.Surveys.Select(s => s.IdSurvey.ToString()).ToList());
    }
}