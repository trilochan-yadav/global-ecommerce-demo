namespace BFF.API.Business.Interfaces;

public interface IAuthService
{
    /// <summary>
    /// Validates credentials and returns a signed JWT, or null if invalid.
    /// </summary>
    string? GenerateToken(string username, string password);
}
