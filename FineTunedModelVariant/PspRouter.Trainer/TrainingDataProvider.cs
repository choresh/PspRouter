using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using PspRouter.Lib;

namespace PspRouter.Trainer;

public class TrainingDataProvider : ITrainingDataProvider
{
    private readonly ILogger<TrainingDataProvider> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;

    public TrainingDataProvider(ILogger<TrainingDataProvider> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection connection string not found");
    }

    public async Task<IEnumerable<TrainingData>> GetTrainingDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching training data from SQL Server");
        
        var trainingData = new List<TrainingData>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // TODO: Replace with actual query string when available
            var query = @"
                SELECT 
                    Id,
                    SystemPrompt,
                    UserInstruction,
                    ContextJson,
                    ExpectedResponse,
                    CreatedAt,
                    Source,
                    IsValidated
                FROM TrainingData 
                WHERE IsValidated = 1
                ORDER BY CreatedAt DESC";
            
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var data = new TrainingData(
                    Id: reader.GetInt32(0),
                    SystemPrompt: reader.GetString(1),
                    UserInstruction: reader.GetString(2),
                    ContextJson: reader.GetString(3),
                    ExpectedResponse: reader.GetString(4),
                    CreatedAt: reader.GetDateTime(5),
                    Source: reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsValidated: reader.GetBoolean(7)
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
