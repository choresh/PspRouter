using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using OpenAI;
using PspRouter.Lib;
using System.Text;
using System.Text.Json;

namespace PspRouter.Trainer;

// Training service implementation
public class TrainingService : ITrainingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrainingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ITrainingDataProvider _trainingDataProvider;
    private readonly string _apiKey;

    public TrainingService(OpenAIClient openAiClient, ILogger<TrainingService> logger, IConfiguration configuration, ITrainingDataProvider trainingDataProvider)
    {
        _httpClient = new HttpClient();
        _logger = logger;
        _configuration = configuration;
        _trainingDataProvider = trainingDataProvider;
        
        // Get API key from environment
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                  throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> CreateFineTunedModelAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating fine-tuned model...");
        
        try
        {
        // 1. Upload training data
            _logger.LogInformation("Step 1: Uploading training data...");
            var fileId = await UploadTrainingDataAsync(cancellationToken);
            
        // 2. Create fine-tuning job
            _logger.LogInformation("Step 2: Creating fine-tuning job...");
            var jobId = await CreateFineTuningJobAsync(fileId, cancellationToken);
            
            // 3. Monitor job status until completion
            _logger.LogInformation("Step 3: Monitoring fine-tuning job status...");
            var modelId = await MonitorFineTuningJobAsync(jobId, cancellationToken);
            
            _logger.LogInformation("Fine-tuned model created successfully: {ModelId}", modelId);
            return modelId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fine-tuned model");
            throw;
        }
    }
    
    private async Task<string> MonitorFineTuningJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var maxWaitTime = TimeSpan.FromHours(2); // Maximum wait time
        var checkInterval = TimeSpan.FromMinutes(1); // Check every minute
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < maxWaitTime)
        {
            var status = await GetFineTuningJobStatusAsync(jobId, cancellationToken);
            
            switch (status.ToLower())
            {
                case "succeeded":
                    // Get the fine-tuned model ID
                    var fineTuningJob = await GetFineTuningJobDetailsAsync(jobId, cancellationToken);
                    if (fineTuningJob.FineTunedModel != null)
                    {
                        return fineTuningJob.FineTunedModel;
                    }
                    throw new InvalidOperationException("Fine-tuning job succeeded but no model ID was returned");
                
                case "failed":
                case "cancelled":
                    throw new InvalidOperationException($"Fine-tuning job {jobId} {status}");
                
                case "validating_files":
                case "queued":
                case "running":
                    _logger.LogInformation("Fine-tuning job {JobId} is {Status}. Waiting...", jobId, status);
                    await Task.Delay(checkInterval, cancellationToken);
                    break;
                
                default:
                    _logger.LogWarning("Unknown fine-tuning job status: {Status}", status);
                    await Task.Delay(checkInterval, cancellationToken);
                    break;
            }
        }
        
        throw new TimeoutException($"Fine-tuning job {jobId} did not complete within {maxWaitTime}");
    }

    public async Task<string> UploadTrainingDataAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading training data from database");
        
        try
        {
            // Get training data from the database
            var trainingData = await _trainingDataProvider.GetTrainingDataAsync(cancellationToken);
            
            _logger.LogInformation("Retrieved {Count} training data records from database", trainingData.Count());
            
            // Convert training data to JSONL format for OpenAI
            var jsonlContent = ConvertTrainingDataToJsonl(trainingData);
            
            // Create a temporary file for upload
            var tempFilePath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFilePath, jsonlContent, cancellationToken);
            
            _logger.LogInformation("Created temporary training file: {TempFilePath}", tempFilePath);
            
            try
            {
                // Upload to OpenAI Files API using HTTP
                var fileId = await UploadFileToOpenAIAsync(tempFilePath, cancellationToken);
                
                _logger.LogInformation("Successfully uploaded training file to OpenAI. File ID: {FileId}", fileId);
                
                return fileId;
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading training data");
            throw;
        }
    }

    public async Task<string> CreateFineTuningJobAsync(string fileId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating fine-tuning job for file {FileId}", fileId);
        
        try
        {
            // Create fine-tuning job using HTTP API
            var jobId = await CreateFineTuningJobViaHttpAsync(fileId, cancellationToken);
            
            _logger.LogInformation("Successfully created fine-tuning job. Job ID: {JobId}", jobId);
            
            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fine-tuning job for file {FileId}", fileId);
            throw;
        }
    }

    public async Task<string> GetFineTuningJobStatusAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting status for fine-tuning job {JobId}", jobId);
        
        try
        {
            // Get fine-tuning job status from HTTP API
            var jobDetails = await GetFineTuningJobDetailsAsync(jobId, cancellationToken);
            
            _logger.LogInformation("Fine-tuning job {JobId} status: {Status}", jobId, jobDetails.Status);
            
            // Log additional details if available
            if (jobDetails.FineTunedModel != null)
            {
                _logger.LogInformation("Fine-tuned model: {ModelId}", jobDetails.FineTunedModel);
            }
            
            if (jobDetails.Error != null)
            {
                _logger.LogError("Fine-tuning job {JobId} failed with error: {Error}", jobId, jobDetails.Error.Message);
            }
            
            return jobDetails.Status ?? "unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for fine-tuning job {JobId}", jobId);
            throw;
        }
    }

    private string ConvertTrainingDataToJsonl(IEnumerable<TrainingData> trainingData)
    {
        var jsonlLines = new List<string>();
        
        foreach (var data in trainingData)
        {
            // Create training examples from real transaction data
            var systemPrompt = "You are a payment service provider (PSP) routing assistant. Your job is to analyze transaction context and recommend the best PSP for processing.";
            
            var userInstruction = $"Route this transaction: Order {data.OrderId}, Amount: {data.Amount}, PaymentGateway: {data.PaymentGatewayId}, Method: {data.PaymentMethodId}, Currency: {data.CurrencyId}, Country: {data.CountryId}, Card BIN: {data.PaymentCardBin}, 3DS: {data.ThreeDSTypeId}, Tokenized: {data.IsTokenized}";
            
            var contextJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                orderId = data.OrderId,
                amount = data.Amount,
                paymentGatewayId = data.PaymentGatewayId,
                paymentMethodId = data.PaymentMethodId,
                currencyId = data.CurrencyId,
                countryId = data.CountryId,
                paymentCardBin = data.PaymentCardBin,
                threeDSTypeId = data.ThreeDSTypeId,
                isTokenized = data.IsTokenized,
                isRerouted = data.IsReroutedFlag,
                routingRuleId = data.PaymentRoutingRuleId
            });
            
            // Determine success based on status IDs
            var isSuccessful = data.PaymentTransactionStatusId == 5 || // Authorized
                              data.PaymentTransactionStatusId == 7 || // Captured  
                              data.PaymentTransactionStatusId == 9;   // Settled
            
            var statusName = data.PaymentTransactionStatusId switch
            {
                5 => "Authorized",
                7 => "Captured", 
                9 => "Settled",
                11 => "Refused",
                15 => "ChargeBack",
                17 => "Error",
                22 => "AuthorizationFailed",
                _ => $"Status_{data.PaymentTransactionStatusId}"
            };
            
            var expectedResponse = System.Text.Json.JsonSerializer.Serialize(new
            {
                recommendedPsp = data.PaymentGatewayId,
                reasoning = $"Based on transaction context, PSP {data.PaymentGatewayId} was selected and resulted in {statusName}",
                success = isSuccessful,
                status = statusName,
                statusId = data.PaymentTransactionStatusId,
                pspReference = data.PspReference,
                gatewayResponse = data.GatewayResponseCode,
                wasRerouted = data.IsReroutedFlag
            });
            
            var trainingExample = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userInstruction },
                    new { role = "assistant", content = expectedResponse }
                }
            };
            
            var jsonLine = System.Text.Json.JsonSerializer.Serialize(trainingExample);
            jsonlLines.Add(jsonLine);
        }
        
        return string.Join("\n", jsonlLines);
    }
    
    // HTTP-based OpenAI API implementations
    private async Task<string> UploadFileToOpenAIAsync(string filePath, CancellationToken cancellationToken)
    {
        using var formData = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath, cancellationToken));
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        
        formData.Add(fileContent, "file", "training_data.jsonl");
        formData.Add(new StringContent("fine-tune"), "purpose");
        
        var response = await _httpClient.PostAsync("https://api.openai.com/v1/files", formData, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var fileResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        return fileResponse.GetProperty("id").GetString() ?? throw new InvalidOperationException("File ID not returned from OpenAI API");
    }
    
    private async Task<string> CreateFineTuningJobViaHttpAsync(string fileId, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            training_file = fileId,
            model = "gpt-3.5-turbo",
            suffix = "psp-router"
        };
        
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("https://api.openai.com/v1/fine_tuning/jobs", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jobResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        return jobResponse.GetProperty("id").GetString() ?? throw new InvalidOperationException("Job ID not returned from OpenAI API");
    }
    
    private async Task<FineTuningJobDetails> GetFineTuningJobDetailsAsync(string jobId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"https://api.openai.com/v1/fine_tuning/jobs/{jobId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jobResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        return new FineTuningJobDetails
        {
            Id = jobResponse.GetProperty("id").GetString(),
            Status = jobResponse.GetProperty("status").GetString(),
            FineTunedModel = jobResponse.TryGetProperty("fine_tuned_model", out var model) ? model.GetString() : null,
            Error = jobResponse.TryGetProperty("error", out var error) ? new FineTuningError { Message = error.GetProperty("message").GetString() } : null
        };
    }
    
    // Helper classes for API responses
    private class FineTuningJobDetails
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public string? FineTunedModel { get; set; }
        public FineTuningError? Error { get; set; }
    }
    
    private class FineTuningError
    {
        public string? Message { get; set; }
    }
}
