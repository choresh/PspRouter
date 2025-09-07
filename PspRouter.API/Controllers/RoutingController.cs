using Microsoft.AspNetCore.Mvc;
using PspRouter.Lib;
using System.Text.Json;

namespace PspRouter.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutingController : ControllerBase
{
    private readonly PspRouter.Lib.PspRouter _router;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(PspRouter.Lib.PspRouter router, ILogger<RoutingController> logger)
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
            _logger.LogInformation("Processing routing request for merchant {MerchantId}, amount {Amount} {Currency}", 
                request.Transaction.MerchantId, request.Transaction.Amount, request.Transaction.Currency);

            var context = new PspRouter.Lib.RouteContext(
                request.Transaction,
                request.Candidates,
                request.Preferences ?? new Dictionary<string, string>(),
                request.Statistics ?? new Dictionary<string, double>()
            );

            var decision = await _router.DecideAsync(context, CancellationToken.None);

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
    /// Update the router with transaction outcome for learning
    /// </summary>
    /// <param name="outcome">Transaction outcome details</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("outcome")]
    public async Task<ActionResult> UpdateOutcome([FromBody] TransactionOutcome outcome)
    {
        try
        {
            _logger.LogInformation("Updating outcome for decision {DecisionId}, PSP {PSP}, Authorized: {Authorized}", 
                outcome.DecisionId, outcome.PspName, outcome.Authorized);

            // Note: We need the original decision to update the reward
            // In a real implementation, you might store decisions and retrieve them here
            // For now, we'll log the outcome for learning purposes
            _logger.LogInformation("Transaction outcome: {Outcome}", JsonSerializer.Serialize(outcome));

            // Simulate async operation
            await Task.Delay(1);

            return Ok(new { message = "Outcome recorded successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction outcome");
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
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "PspRouter.API",
            version = "1.0.0"
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

    /// <summary>
    /// Routing preferences
    /// </summary>
    public Dictionary<string, string>? Preferences { get; set; }

    /// <summary>
    /// Historical statistics
    /// </summary>
    public Dictionary<string, double>? Statistics { get; set; }
}
