using Microsoft.AspNetCore.Mvc;
using PspRouter.Lib;
using System.Reflection;

namespace PspRouter.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RoutingController : ControllerBase
{
    private readonly Lib.PspRouter _router;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(Lib.PspRouter router, ILogger<RoutingController> logger)
    {
        _router = router;
        _logger = logger;
    }

    /// <summary>
    /// Route a payment transaction to the best PSP
    /// </summary>
    /// <param name="request">Routing request containing transaction details and candidates</param>
    /// <returns>Routing decision with selected PSP and reasoning</returns>
    [HttpPost("route")]
    public async Task<ActionResult<RouteDecision>> RouteTransaction([FromBody] RouteRequest request)
    {
        try
        {
            _logger.LogInformation("Processing routing request for merchant {MerchantId}, amount {Amount} {CurrencyId}", 
                request.Transaction.MerchantId, request.Transaction.Amount, request.Transaction.CurrencyId);

            var context = new Lib.RouteContext(
                request.Transaction,
                request.Candidates
            );

            var decision = await _router.Decide(context, CancellationToken.None);

            _logger.LogInformation("Routing decision made: {PSP} - {Reasoning}", decision.Candidate, decision.Reasoning);

            return Ok(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing routing request");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    

    /// <summary>
    /// Get health status of the routing service
    /// </summary>
    /// <returns>Service health information</returns>
    [HttpGet("health")]
    public ActionResult<object> GetHealth()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "PspRouter.API",
            version = informationalVersion,
            assemblyVersion = version,
            apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0"
        });
    }
}

/// <summary>
/// Request model for routing a transaction
/// </summary>
public class RouteRequest
{
    /// <summary>
    /// Transaction details
    /// </summary>
    public RouteInput Transaction { get; set; } = null!;

    /// <summary>
    /// Available PSP candidates
    /// </summary>
    public List<PspSnapshot> Candidates { get; set; } = new();

}
