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
    private readonly MLBasedRouter _mlRouter;
    private readonly IMLPredictionService _mlPredictionService;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(Lib.PspRouter router, MLBasedRouter mlRouter, IMLPredictionService mlPredictionService, ILogger<RoutingController> logger)
    {
        _router = router;
        _mlRouter = mlRouter;
        _mlPredictionService = mlPredictionService;
        _logger = logger;
    }

    /// <summary>
    /// Route a payment transaction to the best PSP using LLM-based routing
    /// </summary>
    /// <param name="request">Routing request containing transaction details and candidates</param>
    /// <returns>Routing decision with selected PSP and reasoning</returns>
    [HttpPost("route")]
    public async Task<ActionResult<RouteDecision>> RouteTransaction([FromBody] RouteRequest request)
    {
        try
        {
            _logger.LogInformation("Processing LLM routing request for merchant {MerchantId}, amount {Amount} {CurrencyId}", 
                request.Transaction.MerchantId, request.Transaction.Amount, request.Transaction.CurrencyId);

            var context = new Lib.RouteContext(
                request.Transaction,
                request.Candidates
            );

            var decision = await _router.Decide(context, CancellationToken.None);

            _logger.LogInformation("LLM routing decision made: {PSP} - {Reasoning}", decision.Candidate, decision.Reasoning);

            return Ok(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LLM routing request");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Route a payment transaction to the best PSP using ML-based routing with LLM fallback
    /// </summary>
    /// <param name="request">Routing request containing transaction details and candidates</param>
    /// <returns>Routing decision with selected PSP and reasoning</returns>
    [HttpPost("route-ml")]
    public async Task<ActionResult<RouteDecision>> RouteTransactionML([FromBody] RouteRequest request)
    {
        try
        {
            _logger.LogInformation("Processing ML routing request for merchant {MerchantId}, amount {Amount} {CurrencyId}", 
                request.Transaction.MerchantId, request.Transaction.Amount, request.Transaction.CurrencyId);

            var context = new Lib.RouteContext(
                request.Transaction,
                request.Candidates
            );

            var decision = await _mlRouter.Decide(context, CancellationToken.None);

            _logger.LogInformation("ML routing decision made: {PSP} - {Reasoning}", decision.Candidate, decision.Reasoning);

            return Ok(decision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ML routing request");
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

    /// <summary>
    /// Get ML model status and information
    /// </summary>
    /// <returns>ML model status information</returns>
    [HttpGet("ml-status")]
    public ActionResult<object> GetMLStatus()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            mlModel = new
            {
                loaded = _mlPredictionService.IsModelLoaded,
                version = "1.0",
                type = "LightGBM",
                description = "PSP routing success prediction model"
            },
            service = "PspRouter.API",
            version = informationalVersion,
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
