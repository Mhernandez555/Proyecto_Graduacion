using System.Security.Cryptography;
namespace Controlex.Web.Services;

/// <summary>PBKDF2-SHA256. Formato: v1.iteraciones.salt.hash (cabe en nvarchar(256)).</summary>
public static class PasswordHasher
{
    private const int Iteraciones = 210_000;

    public static string Hash(string clave)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var resumenCriptografico = Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"v1.{Iteraciones}.{Convert.ToBase64String(sal)}.{Convert.ToBase64String(resumenCriptografico)}";
    }

    public static bool Verify(string clave, string almacenado)
    {
        var partes = almacenado.Split('.');
        if (partes.Length != 4 || partes[0] != "v1" || !int.TryParse(partes[1], out var iteraciones)) return false;
        var sal = Convert.FromBase64String(partes[2]);
        var hashEsperado = Convert.FromBase64String(partes[3]);
        var hashCalculado = Rfc2898DeriveBytes.Pbkdf2(clave, sal, iteraciones, HashAlgorithmName.SHA256, hashEsperado.Length);
        return CryptographicOperations.FixedTimeEquals(hashCalculado, hashEsperado);
    }
}
