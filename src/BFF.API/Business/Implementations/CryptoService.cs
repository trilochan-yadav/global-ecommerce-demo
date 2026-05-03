using BFF.API.Business.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BFF.API.Business.Implementations;

/// <summary>
/// AES-256-GCM symmetric encryption service.
/// The Angular client fetches the key via GET /api/auth/public-key, encrypts the
/// payment token in the browser using the Web Crypto API, and sends the encrypted
/// payload. The BFF decrypts before forwarding to Order.API so plain tokens never
/// travel over the external network.
///
/// NOTE: For true PCI-DSS compliance use asymmetric RSA — client encrypts with
/// the server public key, only the server private key can decrypt. This demo
/// illustrates the application-layer encryption pattern.
/// </summary>
public class CryptoService : ICryptoService
{
    private readonly byte[] _key;

    public CryptoService(IConfiguration config)
    {
        var base64Key = config["EncryptionKey"]
            ?? throw new InvalidOperationException("EncryptionKey is not configured.");

        _key = Convert.FromBase64String(base64Key);

        if (_key.Length != 32)
            throw new InvalidOperationException("EncryptionKey must decode to exactly 32 bytes (AES-256).");
    }

    /// <inheritdoc/>
    public string GetKeyBase64() => Convert.ToBase64String(_key);

    /// <inheritdoc/>
    public string Decrypt(string encryptedToken)
    {
        var parts = encryptedToken.Split(':');
        if (parts.Length != 2)
            throw new ArgumentException("Invalid encrypted token format. Expected base64iv:base64ciphertext.");

        var iv = Convert.FromBase64String(parts[0]);              // 12 bytes (GCM nonce)
        var ciphertextWithTag = Convert.FromBase64String(parts[1]);

        const int tagLength = 16;
        if (ciphertextWithTag.Length <= tagLength)
            throw new ArgumentException("Ciphertext too short to contain authentication tag.");

        var ciphertext = ciphertextWithTag[..^tagLength];
        var tag = ciphertextWithTag[^tagLength..];

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, tagLength);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
