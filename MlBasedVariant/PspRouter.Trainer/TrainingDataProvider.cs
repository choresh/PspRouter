using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace PspRouter.Trainer;

public class TrainingDataProvider : ITrainingDataProvider
{
    private readonly ILogger<TrainingDataProvider> _logger;
    private readonly string _connectionString;
    private readonly TrainerSettings _settings;

    public TrainingDataProvider(ILogger<TrainingDataProvider> logger, TrainerSettings settings)
    {
        _logger = logger;
        _settings = settings;

        // Get .env file variable
        var baseConnectionString = Environment.GetEnvironmentVariable("PSPROUTER_DB_CONNECTION") ?? throw new InvalidOperationException("PSPROUTER_DB_CONNECTION environment variable is required");
        
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

    public async Task<IEnumerable<TrainingData>> GetTrainingData(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching training data from SQL Server");
        
        var trainingData = new List<TrainingData>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Query to fetch diverse transaction data with configurable date window, per-segment cap,
            // and success/failure balance (two-stage union with caps)
            var query = @"
                WITH RankedTransactions AS (
                    SELECT 
                        pt.PaymentTransactionId,
                        pt.OrderId,
                        pt.Amount,
                        pt.PaymentGatewayId,
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
                        ROW_NUMBER() OVER (
                            PARTITION BY 
                                pt.PaymentGatewayId, 
                                pt.CountryId, 
                                pt.PaymentMethodId,
                                CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END
                            ORDER BY pt.DateCreated DESC
                        ) as DiversityRank
                    FROM PaymentTransactions pt
                    WHERE pt.DateCreated >= DATEADD(MONTH, -@Months, GETDATE())
                        AND pt.PaymentGatewayId IS NOT NULL
                        AND pt.PaymentTransactionStatusId IN (5, 7, 9, 11, 15, 17, 22)
                        AND pt.CountryId IS NOT NULL
                        AND pt.PaymentMethodId IS NOT NULL
                ),
                DiverseSample AS (
                    SELECT *
                    FROM RankedTransactions
                    WHERE DiversityRank <= @MaxPerSegment
                ),
                SuccessSample AS (
                    SELECT TOP (@TopSuccess) *
                    FROM DiverseSample
                    WHERE IsSuccess = 1
                    ORDER BY NEWID()
                ),
                FailureSample AS (
                    SELECT TOP (@TopFailure) *
                    FROM DiverseSample
                    WHERE IsSuccess = 0
                    ORDER BY NEWID()
                )
                SELECT 
                    PaymentTransactionId,
                    OrderId,
                    Amount,
                    PaymentGatewayId,
                    PaymentMethodId,
                    CurrencyId,
                    CountryId,
                    PaymentCardBin,
                    ThreeDSTypeId,
                    IsTokenized,
                    PaymentTransactionStatusId,
                    GatewayStatusReason,
                    GatewayResponseCode,
                    IsReroutedFlag,
                    PaymentRoutingRuleId,
                    DateCreated,
                    DateStatusLastUpdated,
                    PspReference
                FROM (
                    SELECT * FROM SuccessSample
                    UNION ALL
                    SELECT * FROM FailureSample
                ) s";
            
            using var command = new SqlCommand(query, connection);
            var topSuccess = (int)Math.Round(_settings.TargetSampleSize * _settings.TargetSuccessRatio);
            var topFailure = Math.Max(0, _settings.TargetSampleSize - topSuccess);
            command.Parameters.AddWithValue("@Months", _settings.DateWindowMonths);
            command.Parameters.AddWithValue("@MaxPerSegment", _settings.MaxPerSegment);
            command.Parameters.AddWithValue("@TopSuccess", topSuccess);
            command.Parameters.AddWithValue("@TopFailure", topFailure);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var data = new TrainingData(
                    PaymentTransactionId: reader.GetInt64(0),
                    OrderId: reader.GetString(1),
                    Amount: reader.GetDecimal(2),
                    PaymentGatewayId: reader.GetInt64(3),
                    PaymentMethodId: reader.GetInt64(4),
                    CurrencyId: reader.GetInt64(5),
                    CountryId: reader.IsDBNull(6) ? null : reader.GetInt64(6),
                    PaymentCardBin: reader.IsDBNull(7) ? null : reader.GetString(7),
                    ThreeDSTypeId: reader.IsDBNull(8) ? null : reader.GetInt64(8),
                    IsTokenized: reader.GetBoolean(9),
                    PaymentTransactionStatusId: reader.GetInt64(10),
                    GatewayStatusReason: reader.IsDBNull(11) ? null : reader.GetString(11),
                    GatewayResponseCode: reader.IsDBNull(12) ? null : reader.GetString(12),
                    IsReroutedFlag: reader.GetBoolean(13),
                    PaymentRoutingRuleId: reader.IsDBNull(14) ? null : reader.GetInt64(14),
                    DateCreated: reader.GetDateTime(15),
                    DateStatusLastUpdated: reader.IsDBNull(16) ? null : reader.GetDateTime(16),
                    PspReference: reader.IsDBNull(17) ? null : reader.GetString(17)
                );
                
                trainingData.Add(data);
            }
            
            _logger.LogInformation("Successfully fetched {Count} training data records", trainingData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching training data from SQL Server");
            throw;
        }
        
        return trainingData;
    }
}
