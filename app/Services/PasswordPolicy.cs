using System.Security.Cryptography;
namespace Controlex.Web.Services;

/// <summary>Complejidad de contraseña (RF-006): letras, números y caracteres especiales.</summary>
public static class PasswordPolicy
{
    public const int Minimo = 8;

    public static string? Validar(string clave)
    {
        if (clave.Length < Minimo) return $"La contraseña debe tener al menos {Minimo} caracteres.";
        if (!clave.Any(char.IsLetter)) return "La contraseña debe incluir letras.";
        if (!clave.Any(char.IsDigit)) return "La contraseña debe incluir números.";
        if (clave.All(char.IsLetterOrDigit)) return "La contraseña debe incluir caracteres especiales (por ejemplo ! @ # $ %).";
        return null;
    }

    public static string GenerarTemporal()
    {
        const string letras = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        const string numeros = "23456789";
        const string especiales = "!@#$%*?";
        var caracteres = new List<char>();
        for (var indice = 0; indice < 5; indice++) caracteres.Add(Tomar(letras));
        for (var indice = 0; indice < 3; indice++) caracteres.Add(Tomar(numeros));
        for (var indice = 0; indice < 2; indice++) caracteres.Add(Tomar(especiales));
        for (var indice = caracteres.Count - 1; indice > 0; indice--)
        {
            var otroIndice = RandomNumberGenerator.GetInt32(indice + 1);
            (caracteres[indice], caracteres[otroIndice]) = (caracteres[otroIndice], caracteres[indice]);
        }
        return new string(caracteres.ToArray());
    }

    private static char Tomar(string caracteres) => caracteres[RandomNumberGenerator.GetInt32(caracteres.Length)];
}
