using Microsoft.AspNetCore.Mvc;
using PspRouter.Lib;
using System.Reflection;

namespace PspRouter.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class RoutingController : ControllerBase
{
    private readonly Router _router;
    private readonly IPredictionService _predictionService;
    private readonly IPspCandidateProvider _candidateProvider;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(Router router, IPredictionService predictionService, IPspCandidateProvider candidateProvider, ILogger<RoutingController> logger)
    {
        _router = router;
        _predictionService = predictionService;
        _candidateProvider = candidateProvider;
        _logger = logger;
    }

    /// <summary>
    /// Route a payment transaction to the best PSP using ML-based routing
    /// </summary>
    /// <param name="request">Simplified routing request containing only transaction details</param>
    /// <returns>Routing decision with selected PSP and reasoning</returns>
    [HttpPost("route")]
    public async Task<ActionResult<RouteDecision>> RouteTransaction([FromBody] SimpleRouteRequest request)
    {
        try
        {
            _logger.LogInformation("Processing ML routing request for merchant {MerchantId}, amount {Amount} {CurrencyId}", 
                request.Transaction.MerchantId, request.Transaction.Amount, request.Transaction.CurrencyId);

            var decision = await _router.Decide(request.Transaction, CancellationToken.None);

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
    /// Submit feedback about transaction execution results for learning
    /// </summary>
    /// <param name="feedback">Transaction execution feedback</param>
    /// <returns>Confirmation of feedback processing</returns>
    [HttpPost("feedback")]
    public async Task<ActionResult<object>> SubmitFeedback([FromBody] TransactionFeedback feedback)
    {
        try
        {
            _logger.LogInformation("Processing feedback for decision {DecisionId}, PSP {PspName}, Authorized: {Authorized}", 
                feedback.DecisionId, feedback.PspName, feedback.Authorized);

            await _candidateProvider.ProcessFeedbackAsync(feedback, CancellationToken.None);

            _logger.LogInformation("Feedback processed successfully for PSP {PspName}", feedback.PspName);

            return Ok(new
            {
                status = "success",
                message = "Feedback processed successfully",
                decisionId = feedback.DecisionId,
                pspName = feedback.PspName,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing feedback for decision {DecisionId}", feedback.DecisionId);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Get current PSP candidates and their performance metrics
    /// </summary>
    /// <returns>List of PSP candidates with performance data</returns>
    [HttpGet("candidates")]
    public async Task<ActionResult<object>> GetCandidates()
    {
        try
        {
            var candidates = await _candidateProvider.GetAllCandidatesAsync(CancellationToken.None);

            var candidateData = candidates.Select(c => new
            {
                name = c.Name,
                supports = c.Supports,
                health = c.Health,
                authRate30d = c.AuthRate30d,
                currentAuthRate = c.CurrentAuthRate,
                feeBps = c.FeeBps,
                fixedFee = c.FixedFee,
                supports3DS = c.Supports3DS,
                tokenization = c.Tokenization,
                performance = new
                {
                    totalTransactions = c.TotalTransactions,
                    successfulTransactions = c.SuccessfulTransactions,
                    averageProcessingTimeMs = c.AverageProcessingTime,
                    lastUpdated = c.LastUpdated
                }
            });

            return Ok(new
            {
                status = "success",
                candidates = candidateData,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PSP candidates");
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
        var version = assembly.GetName().Version?.ToString() ?? throw new InvalidOperationException("Assembly version is required");
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "PspRouter.API",
            version = informationalVersion,
            assemblyVersion = version,
            apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? throw new InvalidOperationException("API version is required")
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
        var version = assembly.GetName().Version?.ToString() ?? throw new InvalidOperationException("Assembly version is required");
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            mlModel = new
            {
                loaded = _predictionService.IsModelLoaded,
                version = "1.0",
                type = "LightGBM",
                description = "PSP routing success prediction model"
            },
            service = "PspRouter.API",
            version = informationalVersion,
            apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? throw new InvalidOperationException("API version is required")
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
