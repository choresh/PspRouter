namespace PspRouter.Lib;

/// <summary>
/// Service for making predictions using the trained ML model
/// </summary>
public interface IPredictionService
{
    Task<MLRoutingPrediction?> PredictBestPsp(RouteContext context, CancellationToken cancellationToken = default);
    bool IsModelLoaded { get; }
}
