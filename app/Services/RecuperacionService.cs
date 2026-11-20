using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace Controlex.Web.Services;

public sealed record DatosRecuperacion(string Correo, string Nombre);

public class RecuperacionService(IConfiguration configuracion)
{
    private readonly string cadenaConexion = configuracion.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Falta la cadena de conexión DefaultConnection.");

    public async Task<DatosRecuperacion?> ObtenerDatosAsync(string nombreUsuario, string cedula)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await using var comando = new SqlCommand("dbo.sp_datos_recuperacion", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };
        comando.Parameters.Add("@nombre_usuario", SqlDbType.NVarChar, 50).Value = nombreUsuario;
        comando.Parameters.Add("@cedula", SqlDbType.NVarChar, 12).Value = cedula;

        await conexion.OpenAsync();
        await using var lector = await comando.ExecuteReaderAsync(CommandBehavior.SingleRow);
        if (!await lector.ReadAsync()) return null;

        return new DatosRecuperacion(
            lector.GetString(0),
            lector.GetString(1));
    }

    public async Task<bool> AplicarAsync(string nombreUsuario, string cedula, string hash, string ip)
    {
        await using var conexion = new SqlConnection(cadenaConexion);
        await using var comando = new SqlCommand("dbo.sp_aplicar_contrasena_temporal", conexion)
        {
            CommandType = CommandType.StoredProcedure
        };
        var resultado = comando.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
        resultado.Direction = ParameterDirection.ReturnValue;
        comando.Parameters.Add("@nombre_usuario", SqlDbType.NVarChar, 50).Value = nombreUsuario;
        comando.Parameters.Add("@cedula", SqlDbType.NVarChar, 12).Value = cedula;
        comando.Parameters.Add("@hash", SqlDbType.NVarChar, 256).Value = hash;
        comando.Parameters.Add("@ip", SqlDbType.NVarChar, 45).Value = ip;

        await conexion.OpenAsync();
        await comando.ExecuteNonQueryAsync();
        return resultado.Value is int aplicado && aplicado == 1;
    }

    public static string GenerarTemporal()
    {
        const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minusculas = "abcdefghijkmnopqrstuvwxyz";
        const string numeros = "23456789";
        const string especiales = "!@#$%*?";
        const string todos = mayusculas + minusculas + numeros + especiales;
        var caracteres = new char[12];

        caracteres[0] = Tomar(mayusculas);
        caracteres[1] = Tomar(minusculas);
        caracteres[2] = Tomar(numeros);
        caracteres[3] = Tomar(especiales);
        for (var indice = 4; indice < caracteres.Length; indice++)
            caracteres[indice] = Tomar(todos);

        for (var indice = caracteres.Length - 1; indice > 0; indice--)
        {
            var otroIndice = RandomNumberGenerator.GetInt32(indice + 1);
            (caracteres[indice], caracteres[otroIndice]) = (caracteres[otroIndice], caracteres[indice]);
        }

        return new string(caracteres);
    }

    private static char Tomar(string caracteres) =>
        caracteres[RandomNumberGenerator.GetInt32(caracteres.Length)];
}
