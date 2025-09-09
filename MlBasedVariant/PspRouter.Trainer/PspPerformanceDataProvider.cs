using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace PspRouter.Trainer;

/// <summary>
/// Data provider for PSP performance model training
/// </summary>
public class PspPerformanceDataProvider
{
    private readonly ILogger<PspPerformanceDataProvider> _logger;
    private readonly string _connectionString;
    private readonly TrainerSettings _settings;

    public PspPerformanceDataProvider(ILogger<PspPerformanceDataProvider> logger, TrainerSettings settings)
    {
        _logger = logger;
        _settings = settings;

        // Get .env file variables
        var baseConnectionString = Environment.GetEnvironmentVariable("PSPROUTER_DB_CONNECTION") 
            ?? throw new InvalidOperationException("PSPROUTER_DB_CONNECTION environment variable is required");
        
        // Ensure TrustServerCertificate=true is included to handle SSL certificate issues
        if (!string.IsNullOrEmpty(baseConnectionString))
        {
            if (!baseConnectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
            {
                _connectionString = baseConnectionString.TrimEnd(';') + ";TrustServerCertificate=true;";
            }
            else
            {
                _connectionString = baseConnectionString;
            }
        }
        else
        {
            _connectionString = string.Empty;
        }
        
        _logger.LogInformation("Using connection string from environment variable (.env file or system environment)");
    }

    public async Task<IEnumerable<PspPerformanceFeatures>> GetPspPerformanceData(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching PSP performance data from SQL Server");
        
        var performanceData = new List<PspPerformanceFeatures>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Much simpler query without complex calculations to avoid timeouts
            var query = @"
                SELECT 
                    pt.PaymentGatewayId,
                    CAST(pt.PaymentGatewayId AS NVARCHAR(50)) as PspName,
                    pt.PaymentTransactionId,
                    pt.OrderId,
                    pt.Amount,
                    pt.PaymentMethodId,
                    pt.CurrencyId,
                    pt.CountryId,
                    pt.PaymentCardBin,
                    pt.ThreeDSTypeId,
                    pt.IsTokenized,
                    pt.PaymentTransactionStatusId,
                    pt.GatewayStatusReason,
                    pt.GatewayResponseCode,
                    pt.IsReroutedFlag,
                    pt.PaymentRoutingRuleId,
                    pt.DateCreated,
                    pt.DateStatusLastUpdated,
                    pt.PspReference,
                    CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END AS IsSuccess,
                    CASE 
                        WHEN pt.DateStatusLastUpdated IS NULL OR pt.DateCreated IS NULL THEN 0
                        WHEN pt.DateStatusLastUpdated < pt.DateCreated THEN 0
                        WHEN DATEDIFF(DAY, pt.DateCreated, pt.DateStatusLastUpdated) > 30 THEN 0
                        WHEN DATEDIFF(DAY, pt.DateCreated, pt.DateStatusLastUpdated) < 0 THEN 0
                        ELSE CAST(DATEDIFF(MINUTE, pt.DateCreated, pt.DateStatusLastUpdated) AS BIGINT) * 60000
                    END AS ProcessingTimeMs,
                    CAST(0.85 AS FLOAT) AS RecentSuccessRate,  -- Default values to avoid complex calculations
                    CAST(2000 AS FLOAT) AS RecentProcessingTime,
                    CAST(0 AS INT) AS RecentVolume
                FROM PaymentTransactions pt
                WHERE pt.DateCreated >= DATEADD(MONTH, -@Months, GETDATE())
                    AND pt.PaymentGatewayId IS NOT NULL
                    AND pt.PaymentTransactionStatusId IN (5, 7, 9, 11, 15, 17, 22)
                    AND pt.CountryId IS NOT NULL
                    AND pt.PaymentMethodId IS NOT NULL
                ORDER BY pt.DateCreated DESC";
            
            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 300; // 5 minutes timeout
            command.Parameters.AddWithValue("@Months", _settings.DateWindowMonths);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var data = new PspPerformanceFeatures
                {
                    // Transaction features
                    Amount = (float)reader.GetDecimal(4),
                    PaymentMethodId = reader.GetInt64(5),
                    CurrencyId = reader.GetInt64(6),
                    CountryId = reader.IsDBNull(7) ? 0 : reader.GetInt64(7),
                    RiskScore = 0, // TODO: Add risk score calculation
                    IsTokenized = reader.GetBoolean(10) ? 1f : 0f,
                    HasThreeDS = reader.IsDBNull(9) ? 0f : 1f, // ThreeDSTypeId presence indicates 3DS
                    IsRerouted = reader.GetBoolean(14) ? 1f : 0f,
                    
                    // PSP identification
                    PspId = reader.GetInt64(0),
                    PspName = reader.GetString(1),
                    
                    // Time-based features
                    HourOfDay = reader.GetDateTime(16).Hour,
                    DayOfWeek = (float)reader.GetDateTime(16).DayOfWeek,
                    DayOfMonth = reader.GetDateTime(16).Day,
                    MonthOfYear = reader.GetDateTime(16).Month,
                    
                    // Historical performance features
                    RecentSuccessRate = Convert.ToSingle(reader.GetValue(21)),
                    RecentProcessingTime = Convert.ToSingle(reader.GetValue(22)),
                    RecentVolume = (float)reader.GetInt32(23),
                    
                    // Labels
                    IsSuccessful = reader.GetInt32(19) == 1,
                    ProcessingTimeMs = (float)reader.GetInt64(20),
                    // Use actual transaction success rate and processing time for health score calculation
                    HealthScore = CalculateHealthScore(
                        reader.GetInt32(19) == 1 ? 1.0f : 0.0f, // Use actual transaction success
                        (float)reader.GetInt64(20) // Use actual processing time
                    ),
                    
                    // Transaction metadata
                    TransactionId = reader.GetInt64(2),
                    DateCreated = reader.GetDateTime(16)
                };
                
                // Calculate derived features
                data.AmountLog = (float)Math.Log10(Math.Max(1, data.Amount));
                data.RiskAdjustedAmount = data.Amount * (1 - data.RiskScore / 100f);
                data.TimeOfDayCategory = CalculateTimeOfDayCategory(data.HourOfDay);
                
                performanceData.Add(data);
            }
            
            _logger.LogInformation("Successfully fetched {Count} PSP performance data records", performanceData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PSP performance data from SQL Server");
            throw;
        }
        
        return performanceData;
    }
    
    private float CalculateHealthScore(float successRate, float processingTime)
    {
        // Simple health scoring: 0=red, 1=yellow, 2=green
        if (successRate >= 0.9 && processingTime <= 2000)
            return 2f; // green
        else if (successRate >= 0.8 && processingTime <= 3000)
            return 1f; // yellow
        else
            return 0f; // red
    }
    
    private float CalculateTimeOfDayCategory(float hourOfDay)
    {
        return hourOfDay switch
        {
            >= 0 and < 6 => 0f,   // night
            >= 6 and < 12 => 1f,  // morning
            >= 12 and < 18 => 2f, // afternoon
            _ => 3f               // evening
        };
    }
}
