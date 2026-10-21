using System.Globalization;
using System.Text.RegularExpressions;

namespace ProyectoETL.Data.Services;

public static class Validador
{
    public static string Texto(string valor, int longitudMaxima, string columna, bool obligatorio = true)
    {
        string limpio = (valor ?? "").Trim();

        if (limpio == "")
        {
            if (obligatorio)
                throw new InvalidDataException($"{columna}: el campo es obligatorio.");
            return "";
        }

        if (limpio.Length > longitudMaxima)
            throw new InvalidDataException($"{columna}: el texto supera los {longitudMaxima} caracteres.");

        return limpio;
    }

    public static int Entero(string valor, string columna)
    {
        if (!int.TryParse((valor ?? "").Trim(), out int numero) || numero <= 0)
            throw new InvalidDataException($"{columna}: '{valor}' no es un entero válido mayor que 0.");

        return numero;
    }

    public static int EnteroConPrefijo(string valor, char prefijo, string columna)
    {
        var coincidencia = Regex.Match(
            (valor ?? "").Trim(),
            "^" + Regex.Escape(prefijo.ToString()) + @"(\d+)$",
            RegexOptions.IgnoreCase);

        if (!coincidencia.Success
            || !int.TryParse(coincidencia.Groups[1].Value, out int numero)
            || numero <= 0)
            throw new InvalidDataException($"{columna}: '{valor}' no tiene el formato esperado ({prefijo} + número).");

        return numero;
    }

    public static int EnteroEnRango(string valor, int minimo, int maximo, string columna)
    {
        if (!int.TryParse((valor ?? "").Trim(), out int numero) || numero < minimo || numero > maximo)
            throw new InvalidDataException($"{columna}: '{valor}' debe ser un entero entre {minimo} y {maximo}.");

        return numero;
    }

    public static DateOnly Fecha(string valor, string columna)
    {
        if (!DateOnly.TryParseExact((valor ?? "").Trim(), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly fecha))
            throw new InvalidDataException($"{columna}: '{valor}' no es una fecha válida (yyyy-MM-dd).");

        return fecha;
    }

    public static string Correo(string valor, string columna)
    {
        string limpio = Texto(valor, 60, columna, obligatorio: false);

        if (limpio != "" && !Regex.IsMatch(limpio, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new InvalidDataException($"{columna}: '{limpio}' no tiene formato de correo.");

        return limpio;
    }
}