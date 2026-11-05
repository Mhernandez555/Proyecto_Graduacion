using System.Security.Cryptography;
namespace Controlex.Web.Services;

/// <summary>PBKDF2-SHA256. Formato: v1.iteraciones.salt.hash (cabe en nvarchar(256)).</summary>
public static class PasswordHasher
{
    private const int Iteraciones = 210_000;

    public static string Hash(string clave)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var h = Rfc2898DeriveBytes.Pbkdf2(clave, salt, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"v1.{Iteraciones}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(h)}";
    }

    public static bool Verify(string clave, string almacenado)
    {
        var p = almacenado.Split('.');
        if (p.Length != 4 || p[0] != "v1" || !int.TryParse(p[1], out var it)) return false;
        var salt = Convert.FromBase64String(p[2]);
        var esperado = Convert.FromBase64String(p[3]);
        var h = Rfc2898DeriveBytes.Pbkdf2(clave, salt, it, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(h, esperado);
    }
}
