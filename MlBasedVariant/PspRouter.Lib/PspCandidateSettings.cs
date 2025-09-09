namespace PspRouter.Lib;

/// <summary>
/// Configuration settings for PSP candidate database loading
/// </summary>
public sealed class PspCandidateSettings
{   
    /// <summary>
    /// Whether to include TrustServerCertificate in connection string
    /// </summary>
    public bool TrustServerCertificate { get; init; }
    
    /// <summary>
    /// SQL query timeout in seconds
    /// </summary>
    public int QueryTimeoutSeconds { get; init; }
    
    /// <summary>
    /// Whether to enable retry logic for database operations
    /// </summary>
    public bool EnableRetry { get; init; }
    
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; init; }
    
    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; init; }
}
