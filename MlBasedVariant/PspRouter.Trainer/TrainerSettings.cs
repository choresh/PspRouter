namespace PspRouter.Trainer;

public sealed class TrainerSettings
{
    // Data sampling settings
    public int TargetSampleSize { get; init; }
    public int DateWindowMonths { get; init; }
    public int MaxPerSegment { get; init; }
    public double TargetSuccessRatio { get; init; }
    
    // Model training settings
    public string ModelOutputPath { get; init; } = string.Empty;
    public int MaxIterations { get; init; }
    public double LearningRate { get; init; }
    public int NumLeaves { get; init; }
    public int MinDataInLeaf { get; init; }
    public double FeatureFraction { get; init; }
    public double BaggingFraction { get; init; }
    public int BaggingFreq { get; init; }
    public double ValidationFraction { get; init; }
    public int EarlyStoppingRounds { get; init; }
    public int Seed { get; init; }
    
    // Training objective
    public string Objective { get; init; } = string.Empty;
    public string Metric { get; init; } = string.Empty;
    
    // PSP Performance Models configuration
    public PspPerformanceModelsConfig PspPerformanceModels { get; init; } = new();
}

public class PspPerformanceModelsConfig
{
    public int SuccessRateMaxIterations { get; init; }
    public double SuccessRateLearningRate { get; init; }
    public int SuccessRateNumLeaves { get; init; }
    public int ProcessingTimeMaxIterations { get; init; }
    public double ProcessingTimeLearningRate { get; init; }
    public int ProcessingTimeNumLeaves { get; init; }
    public int HealthMaxIterations { get; init; }
    public double HealthLearningRate { get; init; }
    public int HealthNumLeaves { get; init; }
    public int MinDataInLeaf { get; init; }
    public double ValidationFraction { get; init; }
    public int EarlyStoppingRounds { get; init; }
    public int Seed { get; init; }
    public string SuccessRateModelPath { get; init; } = string.Empty;
    public string ProcessingTimeModelPath { get; init; } = string.Empty;
    public string HealthModelPath { get; init; } = string.Empty;
}


