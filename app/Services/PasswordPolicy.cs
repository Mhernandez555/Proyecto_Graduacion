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
        var c = new List<char>();
        for (var i = 0; i < 5; i++) c.Add(Tomar(letras));
        for (var i = 0; i < 3; i++) c.Add(Tomar(numeros));
        for (var i = 0; i < 2; i++) c.Add(Tomar(especiales));
        for (var i = c.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (c[i], c[j]) = (c[j], c[i]);
        }
        return new string(c.ToArray());
    }

    private static char Tomar(string s) => s[RandomNumberGenerator.GetInt32(s.Length)];
}
