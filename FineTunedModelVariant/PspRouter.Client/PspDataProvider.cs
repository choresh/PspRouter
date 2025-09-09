using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace PspRouter.Client;

/// <summary>
/// Provides real-time PSP data by querying the PaymentTransaction database
/// </summary>
public interface IPspDataProvider
{
    /// <summary>
    /// Gets all available PSPs with their current performance metrics
    /// </summary>
    Task<List<PspSnapshot>> GetAvailablePsps(CancellationToken ct = default);
    
    /// <summary>
    /// Gets PSPs filtered by specific criteria (currency, payment method, etc.)
    /// </summary>
    Task<List<PspSnapshot>> GetFilteredPsps(
        long? currencyId = null, 
        long? paymentMethodId = null, 
        bool? supports3DS = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets detailed performance metrics for a specific PSP
    /// </summary>
    Task<PspPerformanceMetrics?> GetPspPerformance(string pspName, CancellationToken ct = default);
}

/// <summary>
/// Real-time PSP data provider that queries the PaymentTransaction database
/// </summary>
public class PspDataProvider : IPspDataProvider
{
    private readonly string _connectionString;
    private readonly ILogger<PspDataProvider>? _logger;

    public PspDataProvider(string connectionString, ILogger<PspDataProvider>? logger = null)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        
        // Ensure TrustServerCertificate is set for SQL Server connections
        if (!_connectionString.Contains("TrustServerCertificate"))
        {
            _connectionString += ";TrustServerCertificate=true";
        }
        
        _logger = logger;
    }

    public async Task<List<PspSnapshot>> GetAvailablePsps(CancellationToken ct = default)
    {
        return await GetFilteredPsps(ct: ct);
    }

    public async Task<List<PspSnapshot>> GetFilteredPsps(
        long? currencyId = null, 
        long? paymentMethodId = null, 
        bool? supports3DS = null,
        CancellationToken ct = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            var query = BuildPspQuery(currencyId, paymentMethodId, supports3DS);
            
            _logger?.LogDebug("Executing PSP query: {Query}", query);

            using var command = new SqlCommand(query, connection);
            AddQueryParameters(command, currencyId, paymentMethodId, supports3DS);

            var psps = new List<PspSnapshot>();
            
            using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var psp = MapToPspSnapshot(reader);
                psps.Add(psp);
            }

            _logger?.LogInformation("Retrieved {Count} PSPs from database", psps.Count);
            return psps;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve PSPs from database");
            throw;
        }
    }

    public async Task<PspPerformanceMetrics?> GetPspPerformance(string pspName, CancellationToken ct = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            var query = """
                WITH PspMetrics AS (
                    SELECT 
                        pt.PspReference,
                        COUNT(*) as TotalTransactions30d,
                        SUM(CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) as SuccessfulTransactions30d,
                        CAST(SUM(CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as AuthRate30d,
                        AVG(CAST(pt.FeeBps AS DECIMAL(10,2))) as AverageFeeBps,
                        AVG(CAST(pt.FixedFee AS DECIMAL(10,2))) as AverageFixedFee
                    FROM PaymentTransactions pt
                    WHERE pt.PspReference = @PspName
                        AND pt.CreatedDate >= DATEADD(day, -30, GETDATE())
                        AND pt.PspReference IS NOT NULL
                    GROUP BY pt.PspReference
                ),
                PspMetrics7d AS (
                    SELECT 
                        pt.PspReference,
                        CAST(SUM(CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as AuthRate7d
                    FROM PaymentTransactions pt
                    WHERE pt.PspReference = @PspName
                        AND pt.CreatedDate >= DATEADD(day, -7, GETDATE())
                        AND pt.PspReference IS NOT NULL
                    GROUP BY pt.PspReference
                )
                SELECT 
                    pm.PspReference,
                    pm.AuthRate30d,
                    ISNULL(pm7d.AuthRate7d, 0) as AuthRate7d,
                    pm.TotalTransactions30d,
                    pm.SuccessfulTransactions30d,
                    pm.AverageFeeBps,
                    pm.AverageFixedFee,
                    CASE 
                        WHEN pm.AuthRate30d >= 80 THEN 'green'
                        WHEN pm.AuthRate30d >= 60 THEN 'yellow'
                        ELSE 'red'
                    END as HealthStatus,
                    GETDATE() as LastUpdated
                FROM PspMetrics pm
                LEFT JOIN PspMetrics7d pm7d ON pm.PspReference = pm7d.PspReference
                WHERE pm.PspReference = @PspName
                """;

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PspName", pspName);

            using var reader = await command.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                return new PspPerformanceMetrics(
                    PspName: reader.GetString("PspReference"),
                    AuthRate30d: reader.GetDouble("AuthRate30d"),
                    AuthRate7d: reader.GetDouble("AuthRate7d"),
                    TotalTransactions30d: reader.GetInt32("TotalTransactions30d"),
                    SuccessfulTransactions30d: reader.GetInt32("SuccessfulTransactions30d"),
                    AverageFeeBps: reader.GetDecimal("AverageFeeBps"),
                    AverageFixedFee: reader.GetDecimal("AverageFixedFee"),
                    HealthStatus: reader.GetString("HealthStatus"),
                    LastUpdated: reader.GetDateTime("LastUpdated")
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve performance metrics for PSP {PspName}", pspName);
            throw;
        }
    }

    /// <summary>
    /// Builds the SQL used to fetch PSP candidates.
    ///
    /// Business meaning of precedences:
    /// 1) Performance window and filters (PspPerformance CTE)
    ///    - We first compute recent performance (last 30 days) per PSP. Any
    ///      currency/payment-method filters applied here restrict which
    ///      transactions are considered in performance metrics. This ensures the
    ///      auth-rate and fee aggregates reflect the same segment as the request.
    ///
    /// 2) Capability filters (PspCapabilities CTE)
    ///    - Next we compute capabilities over the same filtered slice. Applying
    ///      currency/payment-method filters here ensures capability presence is
    ///      evaluated within the same segment (e.g., card-only, specific currency).
    ///
    /// 3) Final gates on the joined result (final WHERE)
    ///    - After joining performance with capabilities, we apply gates that
    ///      depend on both (e.g., Supports3DS when SCARequired). These are
    ///      eligibility constraints, not performance derivations.
    ///
    /// 4) Minimum volume and ordering (reliability first)
    ///    - We exclude PSPs with too few transactions in the period to avoid
    ///      unstable metrics (pp.TotalTransactions30d >= 10), then order by
    ///      auth rate to favor historically successful PSPs for the given slice.
    ///
    /// In short: segment the data first (performance → capabilities), then apply
    /// eligibility gates, then reliability/ordering. This sequence avoids leaking
    /// unrelated traffic into metrics and ensures only capable, reliable PSPs are
    /// returned in priority order.
    /// </summary>
    private string BuildPspQuery(long? currencyId, long? paymentMethodId, bool? supports3DS)
    {
        var baseQuery = """
            WITH PspPerformance AS (
                SELECT 
                    pt.PspReference,
                    COUNT(*) as TotalTransactions30d,
                    SUM(CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) as SuccessfulTransactions30d,
                    CAST(SUM(CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) as AuthRate30d,
                    AVG(CAST(pt.FeeBps AS DECIMAL(10,2))) as AverageFeeBps,
                    AVG(CAST(pt.FixedFee AS DECIMAL(10,2))) as AverageFixedFee
                FROM PaymentTransactions pt
                WHERE pt.CreatedDate >= DATEADD(day, -30, GETDATE())
                    AND pt.PspReference IS NOT NULL
                    {0}
                GROUP BY pt.PspReference
            ),
            PspCapabilities AS (
                SELECT 
                    pt.PspReference,
                    COUNT(DISTINCT pt.PaymentMethodId) as PaymentMethodCount,
                    COUNT(DISTINCT pt.CurrencyId) as CurrencyCount,
                    MAX(CASE WHEN pt.ThreeDSTypeId IS NOT NULL THEN 1 ELSE 0 END) as Has3DSSupport,
                    MAX(CASE WHEN pt.IsTokenized = 1 THEN 1 ELSE 0 END) as HasTokenization
                FROM PaymentTransactions pt
                WHERE pt.PspReference IS NOT NULL
                    {1}
                GROUP BY pt.PspReference
            )
            SELECT 
                pp.PspReference as Name,
                1 as Supports,  -- All PSPs in database are considered supported
                CASE 
                    WHEN pp.AuthRate30d >= 80 THEN 'green'
                    WHEN pp.AuthRate30d >= 60 THEN 'yellow'
                    ELSE 'red'
                END as Health,
                pp.AuthRate30d / 100.0 as AuthRate30d,
                CAST(pp.AverageFeeBps AS INT) as FeeBps,
                pp.AverageFixedFee as FixedFee,
                pc.Has3DSSupport as Supports3DS,
                pc.HasTokenization as Tokenization
            FROM PspPerformance pp
            INNER JOIN PspCapabilities pc ON pp.PspReference = pc.PspReference
            WHERE pp.TotalTransactions30d >= 10  -- Minimum transaction volume for reliability
            {2}
            ORDER BY pp.AuthRate30d DESC
            """;

        // Precedence: segment performance first, then capabilities, then final gates
        var performanceWhere = new List<string>();   // affects aggregates (auth rate, fees)
        var capabilitiesWhere = new List<string>();  // affects capability projection (3DS/tokenization)
        var finalWhere = new List<string>();         // eligibility gates on the joined output

        // Add filters for performance query
        if (currencyId.HasValue)
        {
            performanceWhere.Add("AND pt.CurrencyId = @CurrencyId");
        }
        if (paymentMethodId.HasValue)
        {
            performanceWhere.Add("AND pt.PaymentMethodId = @PaymentMethodId");
        }

        // Add filters for capabilities query
        if (currencyId.HasValue)
        {
            capabilitiesWhere.Add("AND pt.CurrencyId = @CurrencyId");
        }
        if (paymentMethodId.HasValue)
        {
            capabilitiesWhere.Add("AND pt.PaymentMethodId = @PaymentMethodId");
        }

        // Add final filters
        if (supports3DS.HasValue)
        {
            finalWhere.Add($"AND pc.Has3DSSupport = {(supports3DS.Value ? 1 : 0)}");
        }

        var performanceWhereClause = performanceWhere.Any() ? string.Join(" ", performanceWhere) : "";
        var capabilitiesWhereClause = capabilitiesWhere.Any() ? string.Join(" ", capabilitiesWhere) : "";
        var finalWhereClause = finalWhere.Any() ? string.Join(" ", finalWhere) : "";

        return string.Format(baseQuery, performanceWhereClause, capabilitiesWhereClause, finalWhereClause);
    }

    private void AddQueryParameters(SqlCommand command, long? currencyId, long? paymentMethodId, bool? supports3DS)
    {
        if (currencyId.HasValue)
        {
            command.Parameters.AddWithValue("@CurrencyId", currencyId.Value);
        }
        if (paymentMethodId.HasValue)
        {
            command.Parameters.AddWithValue("@PaymentMethodId", paymentMethodId.Value);
        }
    }

    private PspSnapshot MapToPspSnapshot(IDataReader reader)
    {
        return new PspSnapshot(
            Name: reader.GetString(0),
            Supports: reader.GetBoolean(1),
            Health: reader.GetString(2),
            AuthRate30d: reader.GetDouble(3),
            FeeBps: reader.GetInt32(4),
            FixedFee: reader.GetDecimal(5),
            Supports3DS: reader.GetBoolean(6),
            Tokenization: reader.GetBoolean(7)
        );
    }
}
