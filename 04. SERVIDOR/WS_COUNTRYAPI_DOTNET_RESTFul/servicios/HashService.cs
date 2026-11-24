using System.Security.Cryptography;
using System.Text;

namespace ec.edu.monster.servicios;

/// <summary>
/// Calcula el hash SHA-256 de las claves. La tabla Usuarios guarda ese hash (64 caracteres
/// hexadecimales en minúsculas), nunca la clave en texto plano.
/// </summary>
/// <remarks>Autor: Escobar Marcos</remarks>
public class HashService
{
    /// <summary>SHA-256 del texto en UTF-8, como 64 caracteres hexadecimales en minúsculas.</summary>
    public string Sha256(string texto)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexStringLower(hash);
    }
}
