using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PspRouter.Client;

/// <summary>
/// Main client for PSP routing with real-time database access
/// </summary>
public class PspRouterClient
{
    private readonly IPspDataProvider _pspProvider;
    private readonly ILogger<PspRouterClient>? _logger;

    public PspRouterClient(IPspDataProvider pspProvider, ILogger<PspRouterClient>? logger = null)
    {
        _pspProvider = pspProvider ?? throw new ArgumentNullException(nameof(pspProvider));
        _logger = logger;
    }

    /// <summary>
    /// Gets all available PSPs with their current performance metrics
    /// </summary>
    public async Task<List<PspSnapshot>> GetAvailablePspsAsync(CancellationToken ct = default)
    {
        try
        {
            var psps = await _pspProvider.GetAvailablePspsAsync(ct);
            _logger?.LogInformation("Retrieved {Count} available PSPs", psps.Count);
            return psps;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve PSPs");
            throw;
        }
    }

    /// <summary>
    /// Gets PSPs filtered by currency and payment method
    /// </summary>
    public async Task<List<PspSnapshot>> GetPspsForTransactionAsync(
        long currencyId, 
        long paymentMethodId, 
        CancellationToken ct = default)
    {
        try
        {
            var psps = await _pspProvider.GetFilteredPspsAsync(
                currencyId: currencyId,
                paymentMethodId: paymentMethodId,
                ct: ct
            );
            
            _logger?.LogInformation("Retrieved {Count} PSPs for CurrencyId={CurrencyId}, PaymentMethodId={PaymentMethodId}", 
                psps.Count, currencyId, paymentMethodId);
            
            return psps;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve filtered PSPs");
            throw;
        }
    }

    /// <summary>
    /// Gets PSPs that support 3DS for high-risk transactions
    /// </summary>
    public async Task<List<PspSnapshot>> Get3DSCapablePspsAsync(CancellationToken ct = default)
    {
        try
        {
            var psps = await _pspProvider.GetFilteredPspsAsync(supports3DS: true, ct: ct);
            
            _logger?.LogInformation("Retrieved {Count} PSPs with 3DS support", psps.Count);
            
            return psps;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve 3DS-capable PSPs");
            throw;
        }
    }

    /// <summary>
    /// Gets detailed performance metrics for a specific PSP
    /// </summary>
    public async Task<PspPerformanceMetrics?> GetPspDetailsAsync(string pspName, CancellationToken ct = default)
    {
        try
        {
            var metrics = await _pspProvider.GetPspPerformanceAsync(pspName, ct);
            
            if (metrics != null)
            {
                _logger?.LogInformation("Retrieved metrics for {PspName}: AuthRate={AuthRate}%, Health={Health}", 
                    pspName, metrics.AuthRate30d, metrics.HealthStatus);
            }
            else
            {
                _logger?.LogWarning("No metrics found for PSP {PspName}", pspName);
            }
            
            return metrics;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve metrics for PSP {PspName}", pspName);
            throw;
        }
    }

    /// <summary>
    /// Builds a route context with PSPs filtered by transaction requirements
    /// </summary>
    public async Task<RouteContext> BuildRouteContextAsync(RouteInput transaction, CancellationToken ct = default)
    {
        try
        {
            // Get PSPs filtered by transaction requirements
            var candidates = await _pspProvider.GetFilteredPspsAsync(
                currencyId: transaction.CurrencyId,
                paymentMethodId: transaction.PaymentMethodId,
                supports3DS: transaction.SCARequired ? true : null,
                ct: ct
            );

            // Filter out unhealthy PSPs
            var healthyCandidates = candidates
                .Where(p => p.Health is "green" or "yellow")
                .ToList();

            _logger?.LogInformation("Built route context with {Count} healthy PSPs for transaction {MerchantId}", 
                healthyCandidates.Count, transaction.MerchantId);

            return new RouteContext(transaction, healthyCandidates);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to build route context for transaction {MerchantId}", transaction.MerchantId);
            throw;
        }
    }

    /// <summary>
    /// Makes a routing decision using a fine-tuned model (requires external model service)
    /// </summary>
    public async Task<RouteDecision> MakeRoutingDecisionAsync(
        RouteInput transaction, 
        string modelApiUrl, 
        string apiKey,
        CancellationToken ct = default)
    {
        try
        {
            // Build route context with real-time PSP data
            var context = await BuildRouteContextAsync(transaction, ct);

            if (context.Candidates.Count == 0)
            {
                return CreateFailureDecision("No valid PSPs available");
            }

            // Call external fine-tuned model service
            var decision = await CallFineTunedModelAsync(context, modelApiUrl, apiKey, ct);

            _logger?.LogInformation("Routing decision made: {PSP} - {Reasoning}", decision.Candidate, decision.Reasoning);
            
            return decision;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to make routing decision for transaction {MerchantId}", transaction.MerchantId);
            throw;
        }
    }

    /// <summary>
    /// Prepares candidates and calls the PspRouter.API for routing decision
    /// </summary>
    public async Task<RouteDecision> MakeDeterministicDecisionAsync(
        RouteInput transaction, 
        string apiBaseUrl, 
        CancellationToken ct = default)
    {
        try
        {
            // Build route context with real-time PSP data
            var context = await BuildRouteContextAsync(transaction, ct);

            if (context.Candidates.Count == 0)
            {
                return CreateFailureDecision("No valid PSPs available");
            }

            // Call the PspRouter.API
            var decision = await CallPspRouterApiAsync(context, apiBaseUrl, ct);

            _logger?.LogInformation("API routing decision: {PSP} - {Reasoning}", decision.Candidate, decision.Reasoning);
            
            return decision;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to make routing decision via API for transaction {MerchantId}", transaction.MerchantId);
            throw;
        }
    }

    private async Task<RouteDecision> CallPspRouterApiAsync(
        RouteContext context, 
        string apiBaseUrl, 
        CancellationToken ct)
    {
        using var httpClient = new HttpClient();
        
        var requestBody = new
        {
            Transaction = context.Transaction,
            Candidates = context.Candidates
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var apiUrl = $"{apiBaseUrl.TrimEnd('/')}/api/routing/route";
        var response = await httpClient.PostAsync(apiUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var decision = JsonSerializer.Deserialize<RouteDecision>(responseJson);

        if (decision == null)
        {
            throw new InvalidOperationException("Failed to deserialize routing decision from API");
        }

        return decision;
    }

    private async Task<RouteDecision> CallFineTunedModelAsync(
        RouteContext context, 
        string modelApiUrl, 
        string apiKey, 
        CancellationToken ct)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var requestBody = new
        {
            Transaction = context.Transaction,
            Candidates = context.Candidates
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true });
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(modelApiUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        var decision = JsonSerializer.Deserialize<RouteDecision>(responseJson);

        if (decision == null)
        {
            throw new InvalidOperationException("Failed to deserialize routing decision from model");
        }

        return decision;
    }

    private RouteDecision CreateFailureDecision(string reason)
    {
        return new RouteDecision(
            Schema_Version: "1.0",
            Decision_Id: Guid.NewGuid().ToString(),
            Candidate: "NONE",
            Alternates: Array.Empty<string>(),
            Reasoning: reason,
            Guardrail: "veto:no_valid_psp",
            Constraints: new RouteConstraints(false, 0, 0),
            Features_Used: Array.Empty<string>()
        );
    }
}

/// <summary>
/// Factory for creating PspRouterClient instances
/// </summary>
public static class PspRouterClientFactory
{
    /// <summary>
    /// Creates a PspRouterClient with connection string
    /// </summary>
    public static PspRouterClient Create(string connectionString, ILogger<PspRouterClient>? logger = null)
    {
        var pspProvider = new PspDataProvider(connectionString, null);
        return new PspRouterClient(pspProvider, logger);
    }

    /// <summary>
    /// Creates a PspRouterClient with custom PSP data provider
    /// </summary>
    public static PspRouterClient Create(IPspDataProvider pspProvider, ILogger<PspRouterClient>? logger = null)
    {
        return new PspRouterClient(pspProvider, logger);
    }
}
