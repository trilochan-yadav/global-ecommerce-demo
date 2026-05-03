using BFF.API.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BFF.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICryptoService _crypto;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService auth, ICryptoService crypto, ILogger<AuthController> logger)
    {
        _auth = auth;
        _crypto = crypto;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user {Username}", request.Username);
        var token = _auth.GenerateToken(request.Username, request.Password);
        if (token is null)
        {
            _logger.LogWarning("Login failed for user {Username}", request.Username);
            return Unauthorized(new { success = false, message = "Invalid credentials" });
        }

        _logger.LogInformation("Login succeeded for user {Username}", request.Username);
        return Ok(new { success = true, token });
    }

    /// <summary>
    /// Returns the AES-256 encryption key so the Angular client can encrypt the
    /// payment token before sending it. The BFF decrypts it before forwarding to
    /// Order.API — plain tokens never travel over the external network.
    /// </summary>
    [HttpGet("public-key")]
    public IActionResult GetPublicKey()
    {
        _logger.LogInformation("Encryption public-key requested");
        return Ok(new { key = _crypto.GetKeyBase64() });
    }
}

public record LoginRequest(string Username, string Password);
