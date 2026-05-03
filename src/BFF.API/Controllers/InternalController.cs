using Microsoft.AspNetCore.Mvc;

namespace BFF.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InternalController : ControllerBase
{
    private readonly ILogger<InternalController> _logger;

    public InternalController(ILogger<InternalController> logger)
    {
        _logger = logger;
    }

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] string service = "BFF.API", [FromQuery] int lines = 100)
    {
        // Normalise: accept both "BFF.API" and "BFFAPI" style names
        var filePrefix = service.Replace(".", "").Replace("-", "");

        // Allowed service names to prevent path traversal
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ProductAPI", "PaymentAPI", "ShippingAPI", "AnalyticsAPI", "OrderAPI", "BFFAPI"
        };

        if (!allowed.Contains(filePrefix))
            return BadRequest(new { error = "Unknown service name" });

        var logsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "logs"));
        if (!Directory.Exists(logsDir))
            return Ok(Array.Empty<string>());

        // Find the most recent log file for this service
        var pattern = $"{filePrefix}-*.log";
        var files = Directory.GetFiles(logsDir, pattern)
            .OrderByDescending(f => f)
            .ToList();

        // Also match the non-dated file (e.g. first startup before rolling)
        var exact = Path.Combine(logsDir, $"{filePrefix}-.log");
        if (System.IO.File.Exists(exact) && !files.Contains(exact))
            files.Insert(0, exact);

        if (files.Count == 0)
            return Ok(Array.Empty<string>());

        var latestFile = files[0];
        string[] allLines;
        try
        {
            // Read with share access so Serilog can still write
            using var fs = new FileStream(latestFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            allLines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read log file for service {Service}", service);
            return StatusCode(500, new { error = "Could not read log file" });
        }

        var result = allLines.TakeLast(lines).Select(l => l.TrimEnd('\r')).ToArray();
        return Ok(result);
    }
}
