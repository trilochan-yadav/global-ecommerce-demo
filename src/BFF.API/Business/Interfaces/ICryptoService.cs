namespace BFF.API.Business.Interfaces;

public interface ICryptoService
{
    /// <summary>Returns the base64-encoded AES-256 key sent to the Angular client for encryption.</summary>
    string GetKeyBase64();

    /// <summary>
    /// Decrypts a token that was AES-256-GCM encrypted by the browser Web Crypto API.
    /// Expected format: base64(iv):base64(ciphertext+tag)
    /// </summary>
    string Decrypt(string encryptedToken);
}
