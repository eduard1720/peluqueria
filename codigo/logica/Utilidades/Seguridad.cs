using System.Security.Cryptography;

namespace Logica.Utilidades;

// Hash de contraseñas con PBKDF2 y sal (RNF-02). Se guarda el texto "sal:hash" en Base64.
public static class Seguridad
{
    private const int BytesSal = 16;
    private const int BytesHash = 32;
    private const int Iteraciones = 100_000;

    public static string CalcularHash(string contrasena)
    {
        byte[] sal = RandomNumberGenerator.GetBytes(BytesSal);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, BytesHash);
        return Convert.ToBase64String(sal) + ":" + Convert.ToBase64String(hash);
    }

    // El hash no se revierte: se recalcula con la misma sal y se comparan los resultados.
    public static bool VerificarHash(string contrasena, string guardado)
    {
        string[] partes = guardado.Split(':');
        if (partes.Length != 2) return false;
        byte[] sal = Convert.FromBase64String(partes[0]);
        byte[] esperado = Convert.FromBase64String(partes[1]);
        byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
