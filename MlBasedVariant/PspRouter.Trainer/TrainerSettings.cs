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
}


